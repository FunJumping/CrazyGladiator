using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using Helper = Neo.SmartContract.Framework.Helper;

using System;
using System.ComponentModel;
using System.Numerics;

namespace AuctionContract
{
    /**
     * smart contract for Auction
     * @author Clyde
     */
    public class Auction : SmartContract
    {
        // global storage
        // auction :         map<tokenid:BigInteger, info:AuctionInfo>           // tokenId to AuctionInfo, key = tokenid
        // money :         map<owner:BigInteger, count:BigInteger>           // money for auction
        // tx recharge:  map<txid:byte[], count:BigInteger>

        // NFT合约hash
        [Appcall("57c819391fab1d794a8146566a55c3b114a3369f")]
        static extern object nftCall(string method, object[] arr);

        // SGAS合约hash
        //[Appcall("e52a08c20986332ad8dccf9ded38cc493878064a")]
        //static extern object nep55Call(string method, object[] arr);
        delegate object deleDyncall(string method, object[] arr);

        // the owner, super admin address
        public static readonly byte[] ContractOwner = "AUGkNMWzBCy5oi1rFKR5sPhpRjhhfgPhU2".ToScriptHash();

        // 有权限发行0代合约的钱包地址
        public static readonly byte[] MintOwner = "AUGkNMWzBCy5oi1rFKR5sPhpRjhhfgPhU2".ToScriptHash();

        // min fee for one transaction
        private const ulong TX_MIN_FEE = 5000000;
        private const ulong GEN0_MAX_PRICE = 1999000000;
        private const ulong GEN0_MIN_PRICE = 199000000;
        private const ulong GEN0_AUCTION_DURATION = 86400;

        // In the auction 正在拍卖中的记录
        [Serializable]
        public class AuctionInfo
        {
            public byte[] owner;
            // 0拍卖 1克隆拍卖
            public int sellType;
            public uint sellTime;
            public BigInteger beginPrice;
            public BigInteger endPrice;
            public BigInteger duration;
        }

        // 0代角斗士成交记录
        public class Gene0Record
        {
            public BigInteger totalSellCount;

            public BigInteger lastPrice0;
            public BigInteger lastPrice1;
            public BigInteger lastPrice2;
            public BigInteger lastPrice3;
            public BigInteger lastPrice4;
        }

        // 拍卖成交记录
        public class AuctionRecord
        {
            public BigInteger tokenId;
            public byte[] seller;
            public byte[] buyer;
            public int sellType;
            public BigInteger sellPrice;
            public BigInteger sellTime;
        }

        //notify 上架拍卖通知
        public delegate void deleAuction(byte[] owner, BigInteger tokenId, BigInteger beginPrice, BigInteger endPrice, BigInteger duration, int sellType, uint sellTime);
        [DisplayName("auction")]
        public static event deleAuction Auctioned;

        //notify 取消拍卖通知
        public delegate void deleCancelAuction(byte[] owner, BigInteger tokenId);
        [DisplayName("cancelAuction")]
        public static event deleCancelAuction CancelAuctioned;

        //notify 购买通知
        public delegate void deleAuctionBuy(byte[] buyer, BigInteger tokenId, BigInteger curBuyPrice, BigInteger fee, BigInteger nowtime);
        [DisplayName("auctionBuy")]
        public static event deleAuctionBuy AuctionBuy;

        //notify 克隆拍卖通知
        public delegate void deleAuctionClone(byte[] buyer, BigInteger motherId, BigInteger fatherId, BigInteger curBuyPrice, BigInteger fee, BigInteger nowtime);
        [DisplayName("auctionClone")]
        public static event deleAuctionClone AuctionClone;

        //notify 和自己的角斗士克隆通知
        public delegate void deleMyClone(byte[] owner, BigInteger motherId, BigInteger fatherId, BigInteger fee, BigInteger nowtime);
        [DisplayName("myClone")]
        public static event deleMyClone MyClone;

        /**
         * Name
         */
        public static string name()
        {
            return "CrazyGladiatorAuction";
        }
        /**
          * 版本
          */
        public static string Version()
        {
            return "1.0.22";
        }


        /**
         * 存储增加的代币数量
         */
        private static void _addTotal(BigInteger count)
        {
            BigInteger total = Storage.Get(Storage.CurrentContext, "totalExchargeSgas").AsBigInteger();
            total += count;
            Storage.Put(Storage.CurrentContext, "totalExchargeSgas", total);
        }
        /**
         * 不包含收取的手续费在内，所有用户存在拍卖行中的代币
         */
        public static BigInteger totalExchargeSgas()
        {
            return Storage.Get(Storage.CurrentContext, "totalExchargeSgas").AsBigInteger();
        }


        /**
         * 存储减少的代币数总量
         */
        private static void _subTotal(BigInteger count)
        {
            BigInteger total = Storage.Get(Storage.CurrentContext, "totalExchargeSgas").AsBigInteger();
            total -= count;
            if (total > 0)
            {
                Storage.Put(Storage.CurrentContext, "totalExchargeSgas", total);
            }
            else
            {

                Storage.Delete(Storage.CurrentContext, "totalExchargeSgas");
            }
        }

        /**
         * 用户在拍卖所存储的代币
         */
        public static BigInteger balanceOf(byte[] address)
        {
            //2018/6/5 cwt 修补漏洞
            byte[] keytaddress = new byte[] { 0x11 }.Concat(address);
            return Storage.Get(Storage.CurrentContext, keytaddress).AsBigInteger();
        }

        /**
         * 该txid是否已经充值过
         */
        public static bool hasAlreadyCharged(byte[] txid)
        {
            //2018/6/5 cwt 修补漏洞
            byte[] keytxid = new byte[] { 0x11 }.Concat(txid);
            byte[] txinfo = Storage.Get(Storage.CurrentContext, keytxid);
            if (txinfo.Length > 0)
            {
                // 已经处理过了
                return false;
            }
            return true;
        }

        /**
         * 使用txid充值
         */
        public static bool rechargeToken(byte[] owner, byte[] txid)
        {
            if (owner.Length != 20)
            {
                Runtime.Log("Owner error.");
                return false;
            }

            //2018/6/5 cwt 修补漏洞
            byte[] keytxid = new byte[] { 0x11 }.Concat(txid);
            byte[] keytowner = new byte[] { 0x11 }.Concat(owner);

            byte[] txinfo = Storage.Get(Storage.CurrentContext, keytxid);
            if (txinfo.Length > 0)
            {
                // 已经处理过了
                return false;
            }


            // 查询交易记录
            object[] args = new object[1] { txid };
            byte[] sgasHash = Storage.Get(Storage.CurrentContext, "sgas");
            deleDyncall dyncall = (deleDyncall)sgasHash.ToDelegate();
            object[] res = (object[])dyncall("getTXInfo", args);

            if (res.Length > 0)
            {
                byte[] from = (byte[])res[0];
                byte[] to = (byte[])res[1];
                BigInteger value = (BigInteger)res[2];

                if (from == owner)
                {
                    if (to == ExecutionEngine.ExecutingScriptHash)
                    {
                        // 标记为处理
                        Storage.Put(Storage.CurrentContext, keytxid, value);

                        BigInteger nMoney = 0;
                        byte[] ownerMoney = Storage.Get(Storage.CurrentContext, keytowner);
                        if (ownerMoney.Length > 0)
                        {
                            nMoney = ownerMoney.AsBigInteger();
                        }
                        nMoney += value;

                        _addTotal(value);

                        // 记账
                        Storage.Put(Storage.CurrentContext, keytowner, nMoney.AsByteArray());
                        return true;
                    }
                }
            }
            return false;
        }

        /**
         * 提币
         */
        public static bool drawToken(byte[] sender, BigInteger count)
        {
            if (sender.Length != 20)
            {
                Runtime.Log("Owner error.");
                return false;
            }

            //2018/6/5 cwt 修补漏洞
            byte[] keytsender = new byte[] { 0x11 }.Concat(sender);

            if (Runtime.CheckWitness(sender))
            {
                BigInteger nMoney = 0;
                byte[] ownerMoney = Storage.Get(Storage.CurrentContext, keytsender);
                if (ownerMoney.Length > 0)
                {
                    nMoney = ownerMoney.AsBigInteger();
                }
                if (count <= 0 || count > nMoney)
                {
                    // 全部提走
                    count = nMoney;
                }

                // 转账
                object[] args = new object[3] { ExecutionEngine.ExecutingScriptHash, sender, count };
                byte[] sgasHash = Storage.Get(Storage.CurrentContext, "sgas");
                deleDyncall dyncall = (deleDyncall)sgasHash.ToDelegate();
                bool res = (bool)dyncall("transfer_app", args);
                if (!res)
                {
                    return false;
                }

                // 记账
                nMoney -= count;

                _subTotal(count);

                if (nMoney > 0)
                {
                    Storage.Put(Storage.CurrentContext, keytsender, nMoney.AsByteArray());
                }
                else
                {
                    Storage.Delete(Storage.CurrentContext, keytsender);
                }

                return true;
            }
            return false;
        }

        /**
         * 创建拍卖
         */
        public static bool createSaleAuction(byte[] tokenOwner, BigInteger tokenId, BigInteger beginPrice, BigInteger endPrice, BigInteger duration)
        {
            return _saleItemWithType(tokenOwner, tokenId, beginPrice, endPrice, duration, 0);
        }

        /**
         * 克隆拍卖创建
         */
        public static bool createCloneAuction(byte[] tokenOwner, BigInteger tokenId, BigInteger beginPrice, BigInteger endPrice, BigInteger duration)
        {
            return _saleItemWithType(tokenOwner, tokenId, beginPrice, endPrice, duration, 1);
        }

        /**
         * sellType:0 拍卖 1克隆拍卖
         */
        private static bool _saleItemWithType(byte[] tokenOwner, BigInteger tokenId, BigInteger beginPrice, BigInteger endPrice, BigInteger duration, int sellType)
        {
            if (tokenOwner.Length != 20)
            {
                Runtime.Log("Owner error.");
                return false;
            }

            if (beginPrice < 0 || endPrice < 0 || beginPrice < endPrice)
            {
                return false;
            }

            if(endPrice < TX_MIN_FEE)
            {
                // 结束价格不能低于最低手续费
                return false;
            }

            //if (Runtime.CheckWitness(tokenOwner))
            // 物品放在拍卖行
            object[] args = new object[3] { tokenOwner, ExecutionEngine.ExecutingScriptHash, tokenId };
            bool res = (bool)nftCall("transferFrom_app", args);
            if (res)
            {
                var nowtime = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;

                AuctionInfo info = new AuctionInfo();
                info.owner = tokenOwner;
                info.sellType = sellType;
                info.sellTime = nowtime;
                info.beginPrice = beginPrice;
                info.endPrice = endPrice;
                info.duration = duration;

                // 入库记录
                _putAuctionInfo(tokenId.AsByteArray(), info);

                // notify
                Auctioned(tokenOwner, tokenId, beginPrice, endPrice, duration, sellType, nowtime);
                return true;
            }

            return false;
        }

        /**
         * 上架0代角斗士
         */
        private static bool _saleGen0(byte[] tokenOwner, BigInteger tokenId, BigInteger beginPrice, BigInteger endPrice, BigInteger duration, int sellType)
        {
            if (beginPrice < 0 || endPrice < 0 || beginPrice < endPrice)
            {
                return false;
            }

            if (endPrice < TX_MIN_FEE)
            {
                // 结束价格不能低于最低手续费
                return false;
            }

            var nowtime = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;

            AuctionInfo info = new AuctionInfo();
            info.owner = tokenOwner;
            info.sellType = sellType;
            info.sellTime = nowtime;
            info.beginPrice = beginPrice;
            info.endPrice = endPrice;
            info.duration = duration;

            // 入库记录
            _putAuctionInfo(tokenId.AsByteArray(), info);

            // notify
            Auctioned(tokenOwner, tokenId, beginPrice, endPrice, duration, sellType, nowtime);
            return true;
        }

        /**
         * 从拍卖场购买,将钱划入合约名下，将物品给买家
         */
        public static bool buyOnAuction(byte[] sender, BigInteger tokenId)
        {
            if (!Runtime.CheckWitness(sender))
            {
                //没有签名
                return false;
            }

            object[] objInfo = _getAuctionInfo(tokenId.AsByteArray());
            if (objInfo.Length > 0)
            {
                AuctionInfo info = (AuctionInfo)(object)objInfo;
                byte[] owner = info.owner;

                var nowtime = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
                var secondPass = nowtime - info.sellTime;
                //var secondPass = (nowtime - info.sellTime) / 1000;
                //2018/6/5 cwt 修补漏洞
                byte[] keytsender = new byte[] { 0x11 }.Concat(sender);
                byte[] keytowner = new byte[] { 0x11 }.Concat(owner);

                BigInteger senderMoney = Storage.Get(Storage.CurrentContext, keytsender).AsBigInteger();
                BigInteger curBuyPrice = computeCurrentPrice(info.beginPrice, info.endPrice, info.duration, secondPass);
                var fee = curBuyPrice * 50 / 1000;
                if (fee < TX_MIN_FEE)
                {
                    fee = TX_MIN_FEE;
                }
                if(curBuyPrice < fee)
                {
                    curBuyPrice = fee;
                }

                if (senderMoney < curBuyPrice)
                {
                    // 钱不够
                    return false;
                }
                

                // 转移物品
                object[] args = new object[3] { ExecutionEngine.ExecutingScriptHash, sender, tokenId };
                bool res = (bool)nftCall("transfer_app", args);
                if (!res)
                {
                    return false;
                }

                // 扣钱
                Storage.Put(Storage.CurrentContext, keytsender, senderMoney - curBuyPrice);

                // 扣除手续费
                BigInteger sellPrice = curBuyPrice - fee;
                _subTotal(fee);

                // 钱记在卖家名下
                BigInteger nMoney = 0;
                byte[] salerMoney = Storage.Get(Storage.CurrentContext, keytowner);
                if (salerMoney.Length > 0)
                {
                    nMoney = salerMoney.AsBigInteger();
                }
                nMoney = nMoney + sellPrice;
                Storage.Put(Storage.CurrentContext, keytowner, nMoney);

                // 删除拍卖记录
                Storage.Delete(Storage.CurrentContext, tokenId.AsByteArray());
                // 成交记录
                /*AuctionRecord record = new AuctionRecord();
                record.tokenId = tokenId;
                record.seller = owner;
                record.buyer = sender;
                record.sellType = 0;
                record.sellPrice = curBuyPrice;
                record.sellTime = nowtime;

                _putAuctionRecord(tokenId.AsByteArray(), record);*/

                if(owner == ContractOwner)
                {
                    Gene0Record gene0Record;
                    byte[] v = (byte[])Storage.Get(Storage.CurrentContext, "gene0Record");
                    if(v.Length==0)
                    {
                        gene0Record = new Gene0Record();
                    }
                    else
                    {
                        object[] infoRec = (object[])Helper.Deserialize(v);
                        gene0Record = (Gene0Record)(object)infoRec;
                    }
                    int idx = (int)gene0Record.totalSellCount % 5;
                    if (idx == 0)
                    {
                        gene0Record.lastPrice0 = curBuyPrice;
                    }
                    else if (idx == 1)
                    {
                        gene0Record.lastPrice1 = curBuyPrice;
                    }
                    else if (idx == 2)
                    {
                        gene0Record.lastPrice1 = curBuyPrice;
                    }
                    else if (idx == 3)
                    {
                        gene0Record.lastPrice1 = curBuyPrice;
                    }
                    else if (idx == 4)
                    {
                        gene0Record.lastPrice1 = curBuyPrice;
                    }

                    gene0Record.totalSellCount += 1;

                    //
                    byte[] infoRec2 = Helper.Serialize(gene0Record);
                    Storage.Put(Storage.CurrentContext, "gene0Record", infoRec2);
                }

                // notify
                AuctionBuy(sender, tokenId, curBuyPrice, fee, nowtime);
                return true;
                
            }
            return false;
        }

        /**
         * 购买拍卖克隆
         */
        public static bool cloneOnAuction(byte[] sender, BigInteger motherGlaId, BigInteger fatherGlaId)
        {
            if (!Runtime.CheckWitness(sender))
            {
                //没有签名
                return false;
            }

            object[] objFatherInfo = _getAuctionInfo(fatherGlaId.AsByteArray());
            if (objFatherInfo.Length > 0)
            {
                AuctionInfo fatherInfo = (AuctionInfo)(object)objFatherInfo;
                byte[] owner = fatherInfo.owner;

                if(fatherInfo.sellType == 1)
                {
                    //2018/6/5 cwt 修补漏洞
                    byte[] keytsender = new byte[] { 0x11 }.Concat(sender);
                    byte[] keytowner = new byte[] { 0x11 }.Concat(owner);

                    var nowtime = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
                    var secondPass = nowtime - fatherInfo.sellTime;
                    //var secondPass = (nowtime - fatherInfo.sellTime) / 1000;

                    BigInteger senderMoney = Storage.Get(Storage.CurrentContext, keytsender).AsBigInteger();
                    BigInteger curBuyPrice = computeCurrentPrice(fatherInfo.beginPrice, fatherInfo.endPrice, fatherInfo.duration, secondPass);
                    var fee = curBuyPrice * 50 / 1000;
                    if (fee < TX_MIN_FEE)
                    {
                        fee = TX_MIN_FEE;
                    }
                    if (curBuyPrice < fee)
                    {
                        curBuyPrice = fee;
                    }

                    if (senderMoney < curBuyPrice)
                    {
                        // 钱不够
                        return false;
                    }

                    // 开始克隆
                    object[] args = new object[3] { sender, motherGlaId, fatherGlaId };
                    bool res = (bool)nftCall("bidOnClone_app", args);
                    if (!res)
                    {
                        return false;
                    }

                    // 扣钱
                    Storage.Put(Storage.CurrentContext, keytsender, senderMoney - curBuyPrice);

                    // 扣除手续费
                    curBuyPrice -= fee;
                    _subTotal(fee);

                    // 钱记在卖家名下
                    BigInteger nMoney = 0;
                    byte[] salerMoney = Storage.Get(Storage.CurrentContext, keytowner);
                    if (salerMoney.Length > 0)
                    {
                        nMoney = salerMoney.AsBigInteger();
                    }
                    nMoney = nMoney + curBuyPrice;
                    Storage.Put(Storage.CurrentContext, keytowner, nMoney);

                    //// 不要删除拍卖记录
                    //Storage.Delete(Storage.CurrentContext, tokenId.AsByteArray());

                    // 成交记录
                    /*AuctionRecord record = new AuctionRecord();
                    record.tokenId = fatherGlaId;
                    record.seller = owner;
                    record.buyer = sender;
                    record.sellType = 1;
                    record.sellPrice = curBuyPrice + fee;
                    record.sellTime = nowtime;

                    _putAuctionRecord(fatherGlaId.AsByteArray(), record);*/

                    // notify
                    AuctionClone(sender, motherGlaId, fatherGlaId, curBuyPrice, fee, nowtime);
                    return true;
                }
            }
            return false;
        }

        /**
         * 和自己的角斗士进行克隆
         */
        public static bool breedWithMy(byte[] sender, BigInteger motherGlaId, BigInteger fatherGlaId)
        {
            if (!Runtime.CheckWitness(sender))
            {
                //没有签名
                return false;
            }

            //2018/6/5 cwt 修补漏洞
            byte[] keytsender = new byte[] { 0x11 }.Concat(sender);

            BigInteger senderOwnerMoney = Storage.Get(Storage.CurrentContext, keytsender).AsBigInteger();
            var fee = TX_MIN_FEE;
            if (senderOwnerMoney < fee)
            {
                // the fee is not enough
                return false;
            }

            // 开始克隆
            object[] args = new object[3] { sender, motherGlaId, fatherGlaId };
            bool res = (bool)nftCall("breedWithMy_app", args);
            if (res)
            {
                // 扣除手续费
                senderOwnerMoney -= fee;
                Storage.Put(Storage.CurrentContext, keytsender, senderOwnerMoney);

                var nowtime = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;

                // notify
                MyClone(sender, motherGlaId, fatherGlaId, fee, nowtime);
                return true;
            }

            return false;
        }

        /**
         * 取消拍卖
         */
        public static bool cancelAuction(byte[] sender, BigInteger tokenId)
        {
            object[] objInfo = _getAuctionInfo(tokenId.AsByteArray());
            if (objInfo.Length > 0)
            {
                AuctionInfo info = (AuctionInfo)(object)objInfo;
                byte[] tokenOwner = info.owner;

                if (sender != tokenOwner)
                {
                    return false;
                }

                if (Runtime.CheckWitness(sender))
                {
                    object[] args = new object[3] { ExecutionEngine.ExecutingScriptHash, tokenOwner, tokenId };
                    bool res = (bool)nftCall("transfer_app", args);
                    if (res)
                    {
                        //Storage.Delete(Storage.CurrentContext, tokenId.AsByteArray());
                        _delAuctionInfo(tokenId.AsByteArray());
                        // notify
                        CancelAuctioned(tokenOwner, tokenId);
                        return true;
                    }
                }
            }
            return false;
        }

        /**
         * 获取拍卖信息
         */
        public static AuctionInfo getAuctionById(BigInteger tokenId)
        {
            object[] objInfo = _getAuctionInfo(tokenId.AsByteArray());
            AuctionInfo info = (AuctionInfo)(object)objInfo;

            return info;
        }

        /**
         * 发布0代角斗士到拍卖场
         */
        public static bool createGen0Auction(byte strength, byte power, byte agile, byte speed,
            byte skill1, byte skill2, byte skill3, byte skill4, byte skill5, byte equip1, byte equip2, byte equip3, byte equip4,
            byte restrictAttribute, byte character, byte part1, byte part2, byte part3, byte part4, byte part5,
            byte appear1, byte appear2, byte appear3, byte appear4, byte appear5, byte chest, byte bracer, byte shoulder,
            byte face, byte lip, byte nose, byte eyes, byte hair)
        {
            byte[] tokenOwner = ExecutionEngine.ExecutingScriptHash;

            if (Runtime.CheckWitness(MintOwner))
            {
                // 
                object[] args = new object[34] { tokenOwner, strength, power, agile, speed,
                skill1, skill2, skill3, skill4, skill5, equip1, equip2, equip3, equip4,
                restrictAttribute, character, part1, part2, part3, part4, part5,
                appear1, appear2, appear3, appear4, appear5, chest, bracer, shoulder,
                face, lip, nose, eyes, hair };
                BigInteger tokenId = (BigInteger)nftCall("createGen0Auction_app", args);
                if (tokenId == 0)
                {

                    return false;
                }

                BigInteger gen0Price = _computeNextGen0Price();
                if(gen0Price < GEN0_MIN_PRICE)
                {
                    gen0Price = GEN0_MIN_PRICE;
                }
                BigInteger beginPrice = gen0Price;
                BigInteger endPrice = GEN0_MIN_PRICE;
                BigInteger duration = GEN0_AUCTION_DURATION;

                return _saleGen0(ContractOwner, tokenId, beginPrice, endPrice, duration, 0);
            }
            return false;
        }

        /**
         * @dev Computes the next gen0 auction starting price, given
         * the average of the past 5 prices + 50%.
         */
        private static BigInteger _computeNextGen0Price()
        {
            BigInteger nextPrice = GEN0_MAX_PRICE;
            
            byte[] v = (byte[])Storage.Get(Storage.CurrentContext, "gene0Record");
            if (v.Length == 0)
            {
                nextPrice = GEN0_MAX_PRICE;
            }
            else
            {
                Gene0Record gene0Record;
                object[] infoRec = (object[])Helper.Deserialize(v);
                gene0Record = (Gene0Record)(object)infoRec;
                BigInteger sum = gene0Record.lastPrice0 + gene0Record.lastPrice1 + gene0Record.lastPrice2 + gene0Record.lastPrice3 + gene0Record.lastPrice4;
                if (gene0Record.totalSellCount < 5)
                {
                    nextPrice = GEN0_MAX_PRICE;
                }
                else
                {
                    nextPrice =(sum / 5) * 3 / 2;
                    if (nextPrice < GEN0_MIN_PRICE)
                    {
                        nextPrice = GEN0_MIN_PRICE;
                    }
                }
            }

            return nextPrice;
        }

        /**
         * 将收入提款到合约拥有者
         */
        public static bool drawToContractOwner(BigInteger count)
        {
            if (Runtime.CheckWitness(ContractOwner))
            {
                BigInteger nMoney = 0;
                // 查询余额
                object[] args = new object[1] { ExecutionEngine.ExecutingScriptHash };
                byte[] sgasHash = Storage.Get(Storage.CurrentContext, "sgas");
                deleDyncall dyncall = (deleDyncall)sgasHash.ToDelegate();
                BigInteger totalMoney = (BigInteger)dyncall("balanceOf", args);
                BigInteger supplyMoney = Storage.Get(Storage.CurrentContext, "totalExchargeSgas").AsBigInteger();

                BigInteger canDrawMax = totalMoney - supplyMoney;
                if (count <= 0 || count > canDrawMax)
                {
                    // 全部提走
                    count = canDrawMax;
                }

                // 转账
                args = new object[3] { ExecutionEngine.ExecutingScriptHash, ContractOwner, count };

                deleDyncall dyncall2 = (deleDyncall)sgasHash.ToDelegate();
                bool res = (bool)dyncall2("transfer_app", args);
                if (!res)
                {
                    return false;
                }

                // 记账
                _subTotal(count);
                return true;
            }
            return false;
        }

        /**
         * 合约入口
         */
        public static Object Main(string method, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification) //取钱才会涉及这里
            {
                if (ContractOwner.Length == 20)
                {
                    // if param ContractOwner is script hash
                    //return Runtime.CheckWitness(ContractOwner);
                    return false;
                }
                else if (ContractOwner.Length == 33)
                {
                    // if param ContractOwner is public key
                    byte[] signature = method.AsByteArray();
                    return VerifySignature(signature, ContractOwner);
                }
            }
            else if (Runtime.Trigger == TriggerType.VerificationR)
            {
                return true;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                //必须在入口函数取得callscript，调用脚本的函数，也会导致执行栈变化，再取callscript就晚了
                var callscript = ExecutionEngine.CallingScriptHash;

                if (method == "cloneOnAuction")
                {
                    if (args.Length != 3) return 0;
                    byte[] sender = (byte[])args[0];
                    BigInteger motherGlaId = (BigInteger)args[1];
                    BigInteger fatherGlaId = (BigInteger)args[2];

                    return cloneOnAuction(sender, motherGlaId, fatherGlaId);
                }

                if (method == "_setSgas")
                {
                    if (Runtime.CheckWitness(ContractOwner))
                    {
                        Storage.Put(Storage.CurrentContext, "sgas", (byte[])args[0]);
                        return new byte[] { 0x01 };
                    }
                    return new byte[] { 0x00 };
                }
                if (method == "getSgas")
                {                  
                    return Storage.Get(Storage.CurrentContext, "sgas");
                }
                //this is in nep5
                if (method == "totalExchargeSgas") return totalExchargeSgas();
                if (method == "version") return Version();
                if (method == "name") return name();
                if (method == "balanceOf")
                {
                    if (args.Length != 1) return 0;
                    byte[] account = (byte[])args[0];
                    return balanceOf(account);
                }

                if (method == "createGen0Auction")
                {
                    if (args.Length != 33) return 0;
                    byte strength = (byte)args[0];
                    byte power = (byte)args[1];
                    byte agile = (byte)args[2];
                    byte speed = (byte)args[3];

                    byte skill1 = (byte)args[4];
                    byte skill2 = (byte)args[5];
                    byte skill3 = (byte)args[6];
                    byte skill4 = (byte)args[7];
                    byte skill5 = (byte)args[8];

                    byte equip1 = (byte)args[9];
                    byte equip2 = (byte)args[10];
                    byte equip3 = (byte)args[11];
                    byte equip4 = (byte)args[12];

                    byte restrictAttribute = (byte)args[13];
                    byte character = (byte)args[14];

                    byte part1 = (byte)args[15];
                    byte part2 = (byte)args[16];
                    byte part3 = (byte)args[17];
                    byte part4 = (byte)args[18];
                    byte part5 = (byte)args[19];
                    byte appear1 = (byte)args[20];
                    byte appear2 = (byte)args[21];
                    byte appear3 = (byte)args[22];
                    byte appear4 = (byte)args[23];
                    byte appear5 = (byte)args[24];
                    byte chest = (byte)args[25];
                    byte bracer = (byte)args[26];
                    byte shoulder = (byte)args[27];
                    byte face = (byte)args[28];
                    byte lip = (byte)args[29];
                    byte nose = (byte)args[30];
                    byte eyes = (byte)args[31];
                    byte hair = (byte)args[32];

                    return createGen0Auction(strength, power, agile, speed,
                        skill1, skill2, skill3, skill4, skill5, equip1, equip2, equip3, equip4,
                        restrictAttribute, character,
                        part1, part2, part3, part4, part5,
                        appear1, appear2, appear3, appear4, appear5, chest, bracer, shoulder,
                        face, lip, nose, eyes, hair);
                }

                if (method == "createSaleAuction")
                {
                    if (args.Length != 5) return 0;
                    byte[] tokenOwner = (byte[])args[0];
                    BigInteger tokenId = (BigInteger)args[1];
                    BigInteger startPrice = (BigInteger)args[2];
                    BigInteger endPrice = (BigInteger)args[3];
                    BigInteger duration = (BigInteger)args[4];

                    return createSaleAuction(tokenOwner, tokenId, startPrice, endPrice, duration);
                }
                if (method == "buyOnAuction")
                {
                    if (args.Length != 2) return 0;
                    byte[] owner = (byte[])args[0];
                    BigInteger tokenId = (BigInteger)args[1];

                    return buyOnAuction(owner, tokenId);
                }
                if (method == "cancelAuction")
                {
                    if (args.Length != 2) return 0;
                    byte[] owner = (byte[])args[0];
                    BigInteger tokenId = (BigInteger)args[1];

                    return cancelAuction(owner, tokenId);
                }
                if (method == "getAuctionById")
                {
                    if (args.Length != 1) return 0;
                    BigInteger tokenId = (BigInteger)args[0];

                    return getAuctionById(tokenId);
                }

                if (method == "createCloneAuction")
                {
                    if (args.Length != 5) return 0;
                    byte[] owner = (byte[])args[0];
                    BigInteger tokenId = (BigInteger)args[1];
                    BigInteger startPrice = (BigInteger)args[2];
                    BigInteger endPrice = (BigInteger)args[3];
                    BigInteger duration = (BigInteger)args[4];

                    return createCloneAuction(owner, tokenId, startPrice, endPrice, duration);
                }

                if (method == "breedWithMy")
                {
                    if (args.Length != 3) return 0;
                    byte[] owner = (byte[])args[0];
                    BigInteger motherGlaId = (BigInteger)args[1];
                    BigInteger fatherGlaId = (BigInteger)args[2];

                    return breedWithMy(owner, motherGlaId, fatherGlaId);
                }

                if (method == "drawToken")
                {
                    if (args.Length != 2) return 0;
                    byte[] owner = (byte[])args[0];
                    BigInteger count = (BigInteger)args[1];

                    return drawToken(owner, count);
                }

                if (method == "drawToContractOwner")
                {
                    if (args.Length != 1) return 0;
                    BigInteger count = (BigInteger)args[0];

                    return drawToContractOwner(count);
                }

                if (method == "rechargeToken")
                {
                    if (args.Length != 2) return 0;
                    byte[] owner = (byte[])args[0];
                    byte[] txid = (byte[])args[1];

                    return rechargeToken(owner, txid);
                }

                if (method == "hasAlreadyCharged")
                {
                    if (args.Length != 1) return 0;
                    byte[] txid = (byte[])args[0];

                    return hasAlreadyCharged(txid);
                }

                if (method == "getAuctionRecord")
                {
                    if (args.Length != 1)
                        return 0;
                    byte[] txid = (byte[])args[0];
                    return getAuctionRecord(txid);
                }
                if (method == "upgrade")//合约的升级就是在合约中要添加这段代码来实现
                {
                    //不是管理员 不能操作
                    if (!Runtime.CheckWitness(ContractOwner))
                        return false;

                    if (args.Length != 1 && args.Length != 9)
                        return false;

                    byte[] script = Blockchain.GetContract(ExecutionEngine.ExecutingScriptHash).Script;
                    byte[] new_script = (byte[])args[0];
                    //如果传入的脚本一样 不继续操作
                    if (script == new_script)
                        return false;

                    byte[] parameter_list = new byte[] { 0x07, 0x10 };
                    byte return_type = 0x05;
                    bool need_storage = (bool)(object)05;
                    string name = "Auction";
                    string version = "1.1";
                    string author = "CG";
                    string email = "0";
                    string description = "test";

                    if (args.Length == 9)
                    {
                        parameter_list = (byte[])args[1];
                        return_type = (byte)args[2];
                        need_storage = (bool)args[3];
                        name = (string)args[4];
                        version = (string)args[5];
                        author = (string)args[6];
                        email = (string)args[7];
                        description = (string)args[8];
                    }
                    Contract.Migrate(new_script, parameter_list, return_type, need_storage, name, version, author, email, description);
                    return true;
                }
            }
            return false;
        }

        /**
		 * Computes the current price of an auction.
		 * @param startingPrice
		 * @param endingPrice
		 * @param duration
		 * @param secondsPassed
		 * @return 
		 */
        private static BigInteger computeCurrentPrice(BigInteger beginPrice, BigInteger endingPrice, BigInteger duration, BigInteger secondsPassed)
        {
            if(duration<1)
            {
                // 避免被0除
                duration = 1;
            }

            if (secondsPassed >= duration)
            {
                // We've reached the end of the dynamic pricing portion
                // of the auction, just return the end price.
                return endingPrice;
            }
            else
            {
                // Starting price can be higher than ending price (and often is!), so
                // this delta can be negative.
                //var totalPriceChange = endingPrice - beginPrice;

                // This multiplication can't overflow, _secondsPassed will easily fit within
                // 64-bits, and totalPriceChange will easily fit within 128-bits, their product
                // will always fit within 256-bits.
                //var currentPriceChange = totalPriceChange * secondsPassed / duration;
                //var currentPrice = beginPrice + (endingPrice - beginPrice) * secondsPassed / duration; 
                return beginPrice + (endingPrice - beginPrice) * secondsPassed / duration;
            }
        }

        /**
         * 获取拍卖信息
         */
        private static object[] _getAuctionInfo(byte[] tokenId)
        {

            byte[] v = Storage.Get(Storage.CurrentContext, tokenId);
            if (v.Length == 0)
                return new object[0];

            /*
            //老式实现方法
            AuctionInfo info = new AuctionInfo();
            int seek = 0;
            var ownerLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.owner = v.AsString().Substring(seek, ownerLen).AsByteArray();
            seek += ownerLen;

            int dataLen;
            dataLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.sellType = v.AsString().Substring(seek, dataLen).AsByteArray().AsBigInteger();

            dataLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.beginPrice = v.AsString().Substring(seek, dataLen).AsByteArray().AsBigInteger();

            dataLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.endPrice = v.AsString().Substring(seek, dataLen).AsByteArray().AsBigInteger();

            dataLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.duration = v.AsString().Substring(seek, dataLen).AsByteArray().AsBigInteger();

            return (object[])(object)info;
            */

            //新式实现方法只要一行
             return (object[])Helper.Deserialize(v);
        }

        /**
         * 存储拍卖信息
         */
        private static void _putAuctionInfo(byte[] tokenId, AuctionInfo info)
        {
            /*
            // 用一个老式实现法
            byte[] auctionInfo = _ByteLen(info.owner.Length).Concat(info.owner);
            auctionInfo = auctionInfo.Concat(_ByteLen(info.sellType.AsByteArray().Length)).Concat(info.sellType.AsByteArray());
            auctionInfo = auctionInfo.Concat(_ByteLen(info.beginPrice.AsByteArray().Length)).Concat(info.beginPrice.AsByteArray());
            auctionInfo = auctionInfo.Concat(_ByteLen(info.endPrice.AsByteArray().Length)).Concat(info.endPrice.AsByteArray());
            auctionInfo = auctionInfo.Concat(_ByteLen(info.duration.AsByteArray().Length)).Concat(info.duration.AsByteArray());
            */
            // 新式实现方法只要一行
            byte[] auctionInfo = Helper.Serialize(info);
    
            Storage.Put(Storage.CurrentContext, tokenId, auctionInfo);
        }

        /**
         * 删除存储拍卖信息
         */
        private static void _delAuctionInfo(byte[] tokenId)
        {
            Storage.Delete(Storage.CurrentContext, tokenId);
        }

        /**
         * 获取拍卖成交记录
         */
        public static object[] getAuctionRecord(byte[] tokenId)
        {
            var key = "buy".AsByteArray().Concat(tokenId);
            byte[] v = Storage.Get(Storage.CurrentContext, key);
            if (v.Length == 0)
            {
                return new object[0];
            }

            /*
            //老式实现方法
            AuctionRecord info = new AuctionRecord();
            int seek = 0;
            var ownerLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.seller = v.AsString().Substring(seek, ownerLen).AsByteArray();
            seek += ownerLen;

            ownerLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.buyer = v.AsString().Substring(seek, ownerLen).AsByteArray();
            seek += ownerLen;

            int dataLen;
            dataLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.sellPrice = v.AsString().Substring(seek, dataLen).AsByteArray().AsBigInteger();

            dataLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.sellTime = v.AsString().Substring(seek, dataLen).AsByteArray().AsBigInteger();

            return (object[])(object)info;
            */

            //新式实现方法只要一行
            return (object[])Helper.Deserialize(v);
        }

        /**
         * 存储拍卖成交记录
         */
        private static void _putAuctionRecord(byte[] tokenId, AuctionRecord info)
        {
            /*
            // 用一个老式实现法
            byte[] auctionInfo = _ByteLen(info.seller.Length).Concat(info.seller);
            auctionInfo = _ByteLen(info.buyer.Length).Concat(info.buyer);
            auctionInfo = auctionInfo.Concat(_ByteLen(info.sellPrice.AsByteArray().Length)).Concat(info.sellPrice.AsByteArray());
            auctionInfo = auctionInfo.Concat(_ByteLen(info.sellTime.AsByteArray().Length)).Concat(info.sellTime.AsByteArray());
            */
            // 新式实现方法只要一行
            byte[] txInfo = Helper.Serialize(info);

            var key = "buy".AsByteArray().Concat(tokenId);
            Storage.Put(Storage.CurrentContext, key, txInfo);
        }

        //private static byte[] _ByteLen(BigInteger n)
        //{
        //    byte[] v = n.AsByteArray();
        //    if (v.Length > 2)
        //        throw new Exception("not support");
        //    if (v.Length < 2)
        //        v = v.Concat(new byte[1] { 0x00 });
        //    if (v.Length < 2)
        //        v = v.Concat(new byte[1] { 0x00 });
        //    return v;
        //}

    }
}
