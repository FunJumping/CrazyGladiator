using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using Helper = Neo.SmartContract.Framework.Helper;

using System;
using System.ComponentModel;
using System.Numerics;

namespace Synthesis
{
    public class Synthesis : SmartContract
    {
        // NFT合约hash
        [Appcall("3d998163a7948a7b8b42e49cf4cf3bfd0db76b57")]
        static extern object nftCall(string method, object[] arr);

        // SGAS合约hash
        //[Appcall("e52a08c20986332ad8dccf9ded38cc493878064a")]
        //static extern object nep55Call(string method, object[] arr);
        delegate object deleDyncall(string method, object[] arr);

        // the owner, super admin address  ARTVr4BMvv5AiGPiLRuAHpENhiYSE4ykGM
        public static readonly byte[] ContractOwner = "ARTVr4BMvv5AiGPiLRuAHpENhiYSE4ykGM".ToScriptHash();
        //出征角斗士拥有者   AVTiBmB3FbNcbTN78tsmDzsJCTEyXWVQ5Y
        public static readonly byte[] ExpeditionOwner = "AVTiBmB3FbNcbTN78tsmDzsJCTEyXWVQ5Y".ToScriptHash();

        /**
         * 远征奖励记录
         */
        [Serializable]
        public class ExpeditionRecord
        {
            public byte[] owner;
            //匹配出征ID
            public BigInteger mid;
            //出征副本ID
            public BigInteger jid;
            //几等奖
            public BigInteger lvl;
            //操作时间
            public uint rTime;
            //gas数量
            public BigInteger gasNum;
            //碎片数量
            public BigInteger debrisNum;
            //门票
            public BigInteger tkV;
            //是否收取
            public BigInteger isRec=0;
        }

        /**
         * 总奖励记录
         */ 
        public class JackpotRecord
        {
            public byte[] owner;
            //出征副本ID
            public BigInteger jid;
            //匹配出征ID
            public BigInteger mid;
            //操作时间
            public uint rTime;
            //奖金数量
            public BigInteger gasNum;
            //是否收取
            public BigInteger isRec;
        }

        /**
         * 出征中奖次数统计
         */
        public class ExpeditionCountRecord
        {
            //出征副本ID
            public BigInteger jid;
            //一等奖个数
            public BigInteger firstCount=0;
            //二等奖个数
            public BigInteger secondCount=0;
            //三等奖个数
            public BigInteger thirdCount=0;
            //安慰奖个数
            public BigInteger comfortCount=0;
            //碎片奖个数
            public BigInteger debrisCount=0;
            //总出征角斗士数量
            public BigInteger allCount=0;
            //总奖池数量(注意这个不能清空)
            public BigInteger totalJackpot;
            //随机因子
            public BigInteger randNum = 1;
        }

        /**
         * 远征基础配置
         */
        public class Ecpedition {
            //一级门票
            public BigInteger baseTkFee;
            //二级门票
            public BigInteger advTkFee;
            public BigInteger firstPrizePR;
            public BigInteger firstPrizeCount;
            public BigInteger secondPrizePR;
            public BigInteger secondGenAward;
            public BigInteger secondAdvAward;
            public BigInteger secondPrizeCount;
            public BigInteger thirdPrizePR;
            public BigInteger thirdGenAward;
            public BigInteger thirdAdvAward;
            public BigInteger thirdPrizeCount;
            public BigInteger comfortPrizePR;
            public BigInteger comfortGenAward;
            public BigInteger comfortAdvAward;
            public BigInteger comfortPrizeCount;
            public BigInteger serviceFeePR;
            //碎片掉率
            public BigInteger debrisBasePR;
            public BigInteger debrisAdvPR;

            public BigInteger factorK1;
            public BigInteger factorK2;
            public BigInteger factorK3;
            public BigInteger factorKbPR;
            public BigInteger factorKcPR;
        }

        //notify 创建远征通知
        public delegate void deleCreateExpedition(BigInteger succ, BigInteger jid,BigInteger ecpState);
        [DisplayName("createExpedition")]
        public static event deleCreateExpedition CreateExpeditioned;
        //notify 远征配置通知
        public delegate void deleSetEcpeditionConfig(BigInteger succ, BigInteger jid, BigInteger ecpState);
        [DisplayName("setEcpeditionConfig")]
        public static event deleSetEcpeditionConfig SetEcpeditionConfiged;
        //notify 出征通知
        public delegate void deleExpedition(byte[] owner, BigInteger succ, BigInteger tokenGId, BigInteger tokenId, BigInteger jid, BigInteger matchId,BigInteger lvl,BigInteger tkV, BigInteger awardV, BigInteger pid);
        [DisplayName("expedition")]
        public static event deleExpedition Expeditioned;
        //notify 击杀BOSS通知
        public delegate void deleKillBoss(BigInteger succ, BigInteger jid, BigInteger jco, byte[] owner_1, BigInteger mid_1, BigInteger jco_1, byte[] owner_2, BigInteger mid_2, BigInteger jco_2, byte[] owner_3, BigInteger mid_3, BigInteger jco_3);
        [DisplayName("killBoss")]
        public static event deleKillBoss KillBossed;
        //notify 领取出征小奖通知
        public delegate void deleAcceptExpedition(byte[] owner, BigInteger succ, BigInteger jid, BigInteger matchId);
        [DisplayName("acceptExpedition")]
        public static event deleAcceptExpedition AcceptExpeditioned;
        
        //notify 领取大奖通知
        public delegate void deleAcceptPrize(byte[] owner,BigInteger succ, BigInteger jid, BigInteger mid);
        [DisplayName("acceptPrize")]
        public static event deleAcceptPrize AcceptPrizeed;

        public static bool setTotalJackpot(BigInteger totalJackpot)
        {
            bool bol = false;
            BigInteger succ = 0;
            BigInteger ecpState = 0;
            BigInteger ecpCode = Storage.Get(Storage.CurrentContext, "ecpCode").AsBigInteger();
            if (Runtime.CheckWitness(ExpeditionOwner))
            {
                byte[] keytaddress = new byte[] { 0x11 }.Concat(ExpeditionOwner);
                BigInteger bal = Storage.Get(Storage.CurrentContext, keytaddress).AsBigInteger();
                if (bal >= totalJackpot) {
                    ExpeditionCountRecord ecpCountRecord;
                    object[] objEcpCountRecord = getExpeditionCountRecord(ecpCode);
                    if (objEcpCountRecord.Length > 0)
                    {
                        bal -= totalJackpot;
                        Storage.Put(Storage.CurrentContext, keytaddress, bal);
                        //
                        ecpCountRecord = (ExpeditionCountRecord)(object)objEcpCountRecord;
                        ecpCountRecord.totalJackpot += totalJackpot;
                        setExpeditionCountRecord(ecpCountRecord);
                        bol = true;
                    }
                }
            }
            return bol;
        }
       
        /**
         * 查询用户牛头人碎片数量
         */
        public static BigInteger balanceOfDebris(byte[] address)
        {
            byte[] keytaddress = "debris".AsByteArray().Concat(address);
            return Storage.Get(Storage.CurrentContext, keytaddress).AsBigInteger();
        }

        /**
         * 保存每局比赛总记录
         */
        private static bool setExpeditionCountRecord(ExpeditionCountRecord ecpCountInfo)
        {
            BigInteger ecpCode = Storage.Get(Storage.CurrentContext, "ecpCode").AsBigInteger();
            var key = "ecpCountInfo".AsByteArray().Concat(ecpCode.AsByteArray());
            byte[] bytesConf = Helper.Serialize(ecpCountInfo);
            Storage.Put(Storage.CurrentContext, key, bytesConf);
            return true;
        }

        /**
         * 查询每局比赛总记录
         */
        public static object[] getExpeditionCountRecord(BigInteger jid)
        {
            var key = "ecpCountInfo".AsByteArray().Concat(jid.AsByteArray());
            byte[] v = Storage.Get(Storage.CurrentContext, key);
            if (v.Length == 0)
            {
                return new object[0];
            }

            return (object[])Helper.Deserialize(v);
        }

        /**
         * 查询远征奖励记录
         */
        public static object[] getExpeditionRecord(BigInteger jid, BigInteger mid)
        {
            var key = jid.AsByteArray().Concat("ecpInfo".AsByteArray()).Concat(mid.AsByteArray());
            byte[] v = Storage.Get(Storage.CurrentContext, key);
            if (v.Length == 0)
            {
                return new object[0];
            }

            return (object[])Helper.Deserialize(v);
        }

        /**
         * 查询远征奖励记录
         */
        public static object[] getJackpotRecord(BigInteger jid, BigInteger mid)
        {
            var key = jid.AsByteArray().Concat("jackpot".AsByteArray()).Concat(mid.AsByteArray());
            byte[] v = Storage.Get(Storage.CurrentContext, key);
            if (v.Length == 0)
            {
                return new object[0];
            }

            return (object[])Helper.Deserialize(v);
        }

        /**
         * 查询远征副本配置信息
         */
        public static object[] getExpedition()
        {
            byte[] v = Storage.Get(Storage.CurrentContext, "ecpConfig");
            if (v.Length == 0)
            {
                return new object[0];
            }

            return (object[])Helper.Deserialize(v);
        }

        /**
         * 创建远征副本
         */
        public static bool createExpedition(BigInteger jid)
        {
            bool bol = false;
            BigInteger ecpCode = 0;
            BigInteger succ = 0;
            BigInteger ecpState = 0;
            if (Runtime.CheckWitness(ExpeditionOwner))
            {
                //设置副本编号
                ecpCode = Storage.Get(Storage.CurrentContext, "ecpCode").AsBigInteger();
                if (ecpCode == jid)
                {
                    //判断副本是否开启，如果正在开启阶段则不能再创建
                    ecpState = Storage.Get(Storage.CurrentContext, "ecpState").AsBigInteger();
                    if (ecpState == 0)
                    {
                        //设置副本编号
                        ecpCode += 1;
                        Storage.Put(Storage.CurrentContext, "ecpCode", ecpCode);
                        //设置副本状态
                        ecpState = 2;
                        Storage.Put(Storage.CurrentContext, "ecpState", ecpState);
                        //计算出新的副本统计数据
                        ExpeditionCountRecord ecpCountRecord2;
                        object[] objEcpCountRecord = getExpeditionCountRecord(jid);
                        BigInteger rand = Blockchain.GetBlock(Blockchain.GetHeight()).ConsensusData;
                        if (objEcpCountRecord.Length > 0)
                        {
                            ExpeditionCountRecord ecpCountRecord = (ExpeditionCountRecord)(object)objEcpCountRecord;
                            ecpCountRecord2 = new ExpeditionCountRecord();
                            ecpCountRecord2.jid = ecpCode;
                            ecpCountRecord2.totalJackpot = ecpCountRecord.totalJackpot;
                        }
                        else
                        {
                            ecpCountRecord2 = new ExpeditionCountRecord();
                            ecpCountRecord2.jid = ecpCode;
                            ecpCountRecord2.totalJackpot = 0;
                        }
                        ecpCountRecord2.randNum = rand;
                        setExpeditionCountRecord(ecpCountRecord2);
                        succ = 1;
                        bol = true;
                    }
                }
            }
            //notify
            CreateExpeditioned(succ, ecpCode, ecpState);
            return bol;
        }

        /**
         * 初始化远征副本配置
         */
        public static bool setEcpeditionConfig(BigInteger baseTkFee,BigInteger advTkFee,BigInteger firstPrizePR,BigInteger firstPrizeCount,BigInteger secondPrizePR,
                                               BigInteger secondGenAward,BigInteger secondAdvAward,BigInteger secondPrizeCount,BigInteger thirdPrizePR,
                                               BigInteger thirdGenAward,BigInteger thirdAdvAward,BigInteger thirdPrizeCount,BigInteger comfortPrizePR,
                                               BigInteger comfortGenAward,BigInteger comfortAdvAward,BigInteger comfortPrizeCount,BigInteger serviceFeePR,
                                               BigInteger debrisBasePR,BigInteger debrisAdvPR,BigInteger factorK1,BigInteger factorK2,BigInteger factorK3,
                                               BigInteger factorKbPR,BigInteger factorKcPR){
            bool bol = false;
            BigInteger ecpCode = 0;
            BigInteger succ = 0;
            BigInteger ecpState = 0;
            if (Runtime.CheckWitness(ExpeditionOwner))
            {

                //ecpState = Storage.Get(Storage.CurrentContext, "ecpState").AsBigInteger();
                //if (ecpState != 2)
                //{
                    Ecpedition ecpInfo = new Ecpedition();  
                    //一级门票
                    ecpInfo.baseTkFee = baseTkFee;
                    //二级门票
                    ecpInfo.advTkFee = advTkFee;
                    //一等奖中奖概率
                    ecpInfo.firstPrizePR = firstPrizePR;
                    //一等奖中奖数量
                    ecpInfo.firstPrizeCount = firstPrizeCount;
                    //二等奖中奖概率
                    ecpInfo.secondPrizePR = secondPrizePR;
                    //二等奖0.25GAS门票奖励
                    ecpInfo.secondGenAward = secondGenAward;
                    //二等奖1GAS门票奖励
                    ecpInfo.secondAdvAward = secondAdvAward;
                    //二等奖中奖数量
                    ecpInfo.secondPrizeCount = secondPrizeCount;
                    //三等奖中奖概率
                    ecpInfo.thirdPrizePR = thirdPrizePR;
                    //三等奖0.25GAS门票奖励
                    ecpInfo.thirdGenAward = thirdGenAward;
                    //三等奖1GAS门票奖励
                    ecpInfo.thirdAdvAward = thirdAdvAward;
                    //三等奖中奖数量
                    ecpInfo.thirdPrizeCount = thirdPrizeCount;
                    //安慰奖中奖概率
                    ecpInfo.comfortPrizePR = comfortPrizePR;
                    //安慰奖0.25GAS门票奖励
                    ecpInfo.comfortGenAward = comfortGenAward;
                    //安慰奖1GAS门票奖励
                    ecpInfo.comfortAdvAward = comfortAdvAward;
                    //安慰奖中奖数量
                    ecpInfo.comfortPrizeCount = comfortPrizeCount;
                    //服务费
                    ecpInfo.serviceFeePR = serviceFeePR;
                    //0.25GAS门票碎片掉率
                    ecpInfo.debrisBasePR = debrisBasePR;
                    //1GAS门票碎片掉率
                    ecpInfo.debrisAdvPR = debrisAdvPR;
                    //K1参数
                    ecpInfo.factorK1 = factorK1;
                    //K2参数
                    ecpInfo.factorK2 = factorK2;
                    //K3参数
                    ecpInfo.factorK3 = factorK3;
                    //Kb参数
                    ecpInfo.factorKbPR = factorKbPR;
                    //Kc参数
                    ecpInfo.factorKcPR = factorKcPR;
                    //设置副本是否开启
                    //Storage.Put(Storage.CurrentContext, "ecpState", 2);
                    //保存配置信息
                    byte[] bytesConf = Helper.Serialize(ecpInfo);
                    Storage.Put(Storage.CurrentContext, "ecpConfig", bytesConf);
                    succ = 1;
                    bol = true;
               //}
            }
            //notify
            SetEcpeditionConfiged(succ, ecpCode, ecpState);
            return bol;
        }

        // 随机数参数
        private const int MAXN = 1 << 20;
        private const ulong randM = MAXN;
        private const ulong randA = 9;  // a = 4p + 1
        private const ulong randB = 7;  // b = 2q + 1

        /**
         * 角斗士远征
         * sender:玩家钱包地址
         * tokenGId:指挥官ID
         * tokenId:出征角斗士ID
         * matchId:出征匹配序号
         * tkType:门票类型1/2
         */
        public static bool expedition(byte[] sender, BigInteger tokenGId, BigInteger tokenId, BigInteger matchId, BigInteger tkType)
        {
            bool bol = false;
            if (!Runtime.CheckWitness(sender))
            {
                //没有签名
                return false;
            }
            //定义notify值
            BigInteger succ = 0;
            BigInteger tkV = 0;
            BigInteger lvl = 4;
            BigInteger awardV = 0;
            ExpeditionCountRecord ecpCountRecord=null;
            BigInteger ecpCode = 0;
            byte[] keytsender = new byte[] { 0x11 }.Concat(sender);
            BigInteger senderMoney = Storage.Get(Storage.CurrentContext, keytsender).AsBigInteger();
            //校验参数合法性
            if (tokenId > 0 && matchId > 0 && tkType > 0)
            {
                //设置副本是否开启
                BigInteger ecpState = Storage.Get(Storage.CurrentContext, "ecpState").AsBigInteger();
                if (ecpState==2)
                {
                    //设置副本编号
                    ecpCode = Storage.Get(Storage.CurrentContext, "ecpCode").AsBigInteger();
                    object[] objEcpInfo = getExpedition();
                    Ecpedition ecpInfo;
                    if (objEcpInfo.Length > 0)
                    {
                        ecpInfo = (Ecpedition)(object)objEcpInfo;
                        if (tkType == 1)
                        {
                            tkV = ecpInfo.baseTkFee;
                        }
                        else
                        {
                            tkV = ecpInfo.advTkFee;
                        }
                        if (tkV <= senderMoney) { 
                            
                            // 查询指挥官加成数据
                            object[] args2 = new object[2] { sender, tokenGId };
                            object[] res2 = (object[])nftCall("getNFTCommander", args2);
                            BigInteger aV = 0;
                            if (res2.Length > 0)
                            {
                                //计算指挥官对GAS的掉落加成
                                BigInteger proV = ((BigInteger)res2[0]);
                                BigInteger genSkV = ((BigInteger)res2[1]);
                                BigInteger advSkV = ((BigInteger)res2[2]);
                                BigInteger genEqV = ((BigInteger)res2[3]);
                                BigInteger advEqV = ((BigInteger)res2[4]);
                                aV = (proV + (advSkV + advEqV) * ecpInfo.factorK1/100000000 + (genSkV + genEqV) * ecpInfo.factorK2 / 100000000) - ecpInfo.factorK3 / 100000000;
                                if (aV < 0)
                                {
                                    aV = 0;
                                }
                            }
                            //计算二等奖概率
                            BigInteger secondV = ecpInfo.secondPrizePR/10000 * (100000000 + aV * ecpInfo.factorKbPR) / 100000000;
                            //计算三等奖概率
                            BigInteger thirdV = ecpInfo.thirdPrizePR/10000 * (100000000 + aV * ecpInfo.factorKbPR) / 100000000 + secondV;
                            //计算安慰奖概率
                            BigInteger comfortV = ecpInfo.comfortPrizePR/10000 * (100000000 + aV * ecpInfo.factorKbPR) / 100000000 + thirdV;
                            //计算碎片掉概率,注意不同的门票概率不一样
                            BigInteger debrisV = ecpInfo.debrisBasePR/10000;
                            if (tkType == 2)
                            {
                                debrisV = ecpInfo.debrisAdvPR/10000;
                            }
                            debrisV += comfortV;
                            //读取获奖数量记录
                            object[] objEcpCountRecord = getExpeditionCountRecord(ecpCode);
                            if (objEcpCountRecord.Length > 0)
                            {
                                ecpCountRecord = (ExpeditionCountRecord)(object)objEcpCountRecord;
                            }
                            else
                            {
                                ecpCountRecord = new ExpeditionCountRecord();
                            }
                            //随机计算中奖概率,在1-10000之间
                            BigInteger rand = ecpCountRecord.randNum;
                            rand = (randA * rand + randB) % randM;
                            BigInteger randV = rand % 10000 + 1;
                            ecpCountRecord.randNum = rand;
                            //
                            if (randV > 0 && randV <= secondV)
                            {
                                //如果超过最大中奖数量则不中奖
                                if (ecpCountRecord.secondCount < ecpInfo.secondPrizeCount)
                                {
                                    succ = 1;
                                    lvl = 2;
                                    if (tkType == 1)
                                    {
                                        awardV = ecpInfo.secondGenAward;
                                    }
                                    else
                                    {
                                        awardV = ecpInfo.secondAdvAward;
                                    }
                                    bol = true;
                                }
                                else
                                {
                                    //未中奖
                                    succ = 1;
                                    lvl = 0;
                                    awardV = 0;
                                    bol = true;
                                }
                            }
                            else if (randV > secondV && randV <= thirdV)
                            {
                                //如果超过最大中奖数量则不中奖
                                if (ecpCountRecord.thirdCount < ecpInfo.thirdPrizeCount)
                                {
                                    succ = 1;
                                    lvl = 3;
                                    if (tkType == 1)
                                    {
                                        awardV = ecpInfo.thirdGenAward;
                                    }
                                    else
                                    {
                                        awardV = ecpInfo.thirdAdvAward;
                                    }
                                    bol = true;
                                }
                                else
                                {
                                    //未中奖
                                    succ = 1;
                                    lvl = 0;
                                    awardV = 0;
                                    bol = true;
                                }
                            }
                            else if (randV > thirdV && randV <= comfortV)
                            {
                                //如果超过最大中奖数量则不中奖
                                if (ecpCountRecord.comfortCount < ecpInfo.comfortPrizeCount)
                                {
                                    succ = 1;
                                    lvl = 4;
                                    if (tkType == 1)
                                    {
                                        awardV = ecpInfo.comfortGenAward;
                                    }
                                    else
                                    {
                                        awardV = ecpInfo.comfortAdvAward;
                                    }
                                    bol = true;
                                }
                                else
                                {
                                    //未中奖
                                    succ = 1;
                                    lvl = 0;
                                    awardV = 0;
                                    bol = true;
                                }
                            }
                            else if (randV > comfortV && randV <= debrisV)
                            {
                                succ = 1;
                                lvl = 5;
                                awardV = 100000000;
                                bol = true;
                            }
                            else
                            {
                                //未中奖
                                succ = 1;
                                lvl = 0;
                                awardV = 0;
                                bol = true;
                            }
                        }   
                    }
                }
            }
            //如果操作成功则扣除门票,保存操作记录
            if (bol == true && succ == 1)
            {
                //转移出征角斗士
                object[] args = new object[4] { sender, ExpeditionOwner, tokenId, ExecutionEngine.ExecutingScriptHash };
                bool res = (bool)nftCall("transfer_Syn", args);
                if (res)
                {
                    //扣除门票
                    senderMoney -= tkV;
                    //减去获奖的金额
                    if (lvl == 2 || lvl == 3 || lvl == 4)
                    {
                        ecpCountRecord.totalJackpot -= awardV;
                    }
                    /* else if(lvl == 5)
                     {
                         byte[] keytaddress = "debris".AsByteArray().Concat(sender);
                         BigInteger debrisTotal = Storage.Get(Storage.CurrentContext, keytaddress).AsBigInteger();
                         debrisTotal += awardV;
                         Storage.Put(Storage.CurrentContext, keytaddress, debrisTotal);
                     }*/
                    senderMoney = senderMoney < 0 ? 0 : senderMoney;
                    Storage.Put(Storage.CurrentContext, keytsender, senderMoney);
                    //进入奖池
                    ecpCountRecord.totalJackpot += tkV;
                    //保存记录
                    var nowtime = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
                    ecpCountRecord.allCount += 1;
                    if (ecpCountRecord.jid == 0)
                    {
                        ecpCountRecord.jid = ecpCode;
                    }
                    if (lvl == 2)
                    {
                        ecpCountRecord.secondCount += 1;
                    }
                    else if (lvl == 3)
                    {
                        ecpCountRecord.thirdCount += 1;
                    }
                    else if (lvl == 4)
                    {
                        ecpCountRecord.comfortCount += 1;
                    }
                    else if (lvl == 5)
                    {
                        ecpCountRecord.debrisCount += 1;
                    }
                    setExpeditionCountRecord(ecpCountRecord);
                    //
                    ExpeditionRecord ecpRecord = new ExpeditionRecord();
                    ecpRecord.owner = sender;
                    ecpRecord.jid = ecpCode;
                    ecpRecord.mid = matchId;
                    ecpRecord.rTime = nowtime;
                    ecpRecord.tkV = tkV;
                    ecpRecord.isRec = 0;
                    ecpRecord.lvl = lvl;
                    if (lvl == 5)
                    {
                        ecpRecord.gasNum = 0;
                        ecpRecord.debrisNum = awardV;
                    }
                    else if (lvl == 0)
                    {
                        ecpRecord.gasNum = 0;
                        ecpRecord.debrisNum = 0;
                    }
                    else
                    {
                        ecpRecord.gasNum = awardV;
                        ecpRecord.debrisNum = 0;
                    }
                    byte[] er = Helper.Serialize(ecpRecord);
                    var key2 = ecpCode.AsByteArray().Concat("ecpInfo".AsByteArray()).Concat(ecpCountRecord.allCount.AsByteArray());
                    Storage.Put(Storage.CurrentContext, key2, er);
                }
                else
                {
                    succ = 0;
                    bol = false;
                }
            }
            //notify
            BigInteger pid = 0;
            if (!ecpCountRecord.Equals(null))
            {
                pid = ecpCountRecord.allCount;
            }
            Expeditioned(sender, succ, tokenGId, tokenId, ecpCode, matchId, lvl, tkV, awardV, pid);
            return bol;
        }

        /**
         * 击杀BOSS开奖
         */
        public static bool killBoss()
        {
            bool bol = false;
            BigInteger ecpCode = 0;
            BigInteger totJ = 0;
            //
            byte[] owner_1 = null;
            BigInteger mid_1 = 0;
            BigInteger jco_1 = 0;
            //
            byte[] owner_2 = null;
            BigInteger mid_2 = 0;
            BigInteger jco_2 = 0;
            //
            byte[] owner_3 = null;
            BigInteger mid_3 = 0;
            BigInteger jco_3 = 0;
            BigInteger randV = 0;
            if (Runtime.CheckWitness(ExpeditionOwner))
            {
                BigInteger ecpState = Storage.Get(Storage.CurrentContext, "ecpState").AsBigInteger();
                if (ecpState == 2)
                { 
                    //设置副本关闭
                    Storage.Put(Storage.CurrentContext, "ecpState", 0);
                    //计算得奖人数
                    BigInteger rand = Blockchain.GetBlock(Blockchain.GetHeight()).ConsensusData;
                    rand = (randA * rand + randB) % randM;
                    randV = rand % 3 + 1;
                    //计算得奖人
                    ecpCode = Storage.Get(Storage.CurrentContext, "ecpCode").AsBigInteger();    
                    ExpeditionCountRecord ecpCountRecord;
                    //读取获奖数量记录
                    object[] objEcpCountRecord = getExpeditionCountRecord(ecpCode);
                    if (objEcpCountRecord.Length > 0)
                    {
                        ecpCountRecord = (ExpeditionCountRecord)(object)objEcpCountRecord;
                        totJ = ecpCountRecord.totalJackpot;
                        BigInteger jco = totJ * 30000000/100000000;
                        BigInteger jco2 = 0;
                        BigInteger[] indexs = new BigInteger[3] {0,0,0};
                        var nowtime = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
                        for (int i = 1; i <= randV; i++)
                        {
                            rand = (randA * rand + randB) % randM;
                            BigInteger index = rand % (ecpCountRecord.allCount - 1 + 1) + 1;
                            //相同mid去重
                            bool st = false;
                            int k = 0;
                            for (k=0;k<3;k++)
                            {
                                if (indexs[k]== index)
                                {
                                    st = true;
                                    break;
                                }
                                else if(indexs[k]==0)
                                {
                                    break;
                                }
                            }
                            if (st==true)
                            {
                                continue;
                            }
                            else
                            {
                                indexs[k] = index;
                            }
                            object[] objEcpRecord = getExpeditionRecord(ecpCode, index);
                            if (objEcpRecord.Length > 0)
                            {
                                ExpeditionRecord ecpRecord = (ExpeditionRecord)(object)objEcpRecord;
                                JackpotRecord jrecord = new JackpotRecord();
                                jrecord.owner = ecpRecord.owner;
                                jrecord.jid = ecpCode;
                                jrecord.rTime = nowtime;
                                jrecord.gasNum = jco / randV * ecpRecord.tkV/100000000;
                                jrecord.isRec = 0;
                                var key = ecpRecord.jid.AsByteArray().Concat("jackpot".AsByteArray()).Concat(ecpRecord.mid.AsByteArray());
                                byte[] er = Helper.Serialize(jrecord);
                                Storage.Put(Storage.CurrentContext, key, er);
                                jco2 += jrecord.gasNum;
                                //
                                if (i==1)
                                {
                                    owner_1 = jrecord.owner;
                                    mid_1 = ecpRecord.mid;
                                    jco_1 = jrecord.gasNum;
                                }else if (i==2)
                                {
                                    owner_2 = jrecord.owner;
                                    mid_2 = ecpRecord.mid;
                                    jco_2 = jrecord.gasNum;
                                }
                                else if (i == 3)
                                {
                                    owner_3 = jrecord.owner;
                                    mid_3 = ecpRecord.mid;
                                    jco_3 = jrecord.gasNum;
                                }
                            }
                        }
                        if (jco2 > 0)
                        {
                            //中奖次数,保存奖池
                            ecpCountRecord.totalJackpot -= jco2;
                            ecpCountRecord.firstCount += randV;
                            setExpeditionCountRecord(ecpCountRecord);
                            //

                        }
                        bol = true;
                    }
                }
            }
            //notify
            if (bol == true)
            {
                KillBossed(1, ecpCode, totJ, owner_1, mid_1, jco_1, owner_2, mid_2, jco_2, owner_3, mid_3, jco_3);
            }
            else
            {
                KillBossed(0, ecpCode, totJ, owner_1, mid_1, jco_1, owner_2, mid_2, jco_2, owner_3, mid_3, jco_3);
            }
            return bol;
        }

        /**
         * 领取远征小奖
         */
        public static bool acceptExpedition(byte[] sender, BigInteger jid, BigInteger pid)
        {
            bool bol = false;
            BigInteger succ = 0;
            ExpeditionRecord ecpRecord = null;
            BigInteger senderMoney = 0;
            if (Runtime.CheckWitness(sender))
            {
                object[] objEcrRecord = getExpeditionRecord(jid, pid);
                if (objEcrRecord.Length > 0)
                {
                    ecpRecord = (ExpeditionRecord)(object)objEcrRecord;
                    if (ecpRecord.isRec == 0 && sender.AsBigInteger() == ecpRecord.owner.AsBigInteger())
                    {
                        byte[] keytsender = new byte[] { 0x11 }.Concat(sender);
                        senderMoney = Storage.Get(Storage.CurrentContext, keytsender).AsBigInteger();
                        //先设置成领取状态
                        ecpRecord.isRec = 1;
                        byte[] er = Helper.Serialize(ecpRecord);
                        var key2 = jid.AsByteArray().Concat("ecpInfo".AsByteArray()).Concat(pid.AsByteArray());
                        Storage.Put(Storage.CurrentContext, key2, er);
                        //费用
                        if (ecpRecord.lvl == 2 || ecpRecord.lvl == 3 || ecpRecord.lvl == 4)
                        {
                            BigInteger prt = ecpRecord.gasNum * 5000000 / 100000000;
                            senderMoney += (ecpRecord.gasNum - prt);
                            _subTotal(prt);
                            //修改用户余额
                            Storage.Put(Storage.CurrentContext, keytsender, senderMoney);
                        }
                        else if (ecpRecord.lvl == 5)
                        {
                            byte[] keytaddress = "debris".AsByteArray().Concat(sender);
                            BigInteger debrisTotal = Storage.Get(Storage.CurrentContext, keytaddress).AsBigInteger();
                            debrisTotal += ecpRecord.debrisNum;
                            Storage.Put(Storage.CurrentContext, keytaddress, debrisTotal);
                        }
                        
                        succ = 1;
                        bol = true;
                    }
                }
            }
            //notify
            AcceptExpeditioned(sender, succ, jid, ecpRecord.mid);
            return bol;
        }

        /**
         * 领取大奖
         */
        public static bool acceptPrize(byte[] sender, BigInteger jid, BigInteger mid)
        {
            bool bol = false;
            BigInteger succ = 0;
            JackpotRecord ecpRecord = null;
            BigInteger senderMoney = 0;
            if (Runtime.CheckWitness(sender))
            {
                object[] objJcoRecord = getJackpotRecord(jid,mid);
                if (objJcoRecord.Length > 0)
                {
                    ecpRecord = (JackpotRecord)(object)objJcoRecord;
                    if (ecpRecord.isRec==0&& sender.AsBigInteger()== ecpRecord.owner.AsBigInteger()) {
                        byte[] keytsender = new byte[] { 0x11 }.Concat(sender);
                        senderMoney = Storage.Get(Storage.CurrentContext, keytsender).AsBigInteger();   
                        //先设置成领取状态
                        ecpRecord.isRec = 1;
                        var key = ecpRecord.jid.AsByteArray().Concat("jackpot".AsByteArray()).Concat(mid.AsByteArray());
                        byte[] er = Helper.Serialize(ecpRecord);
                        Storage.Put(Storage.CurrentContext, key, er);
                        //费用
                        BigInteger prt = ecpRecord.gasNum * 5000000 / 100000000;
                        _subTotal(prt);
                        senderMoney += (ecpRecord.gasNum - prt);
                        //修改
                        Storage.Put(Storage.CurrentContext, keytsender, senderMoney);
                        succ = 1;
                        bol = true;
                    }
                }
            }
            //notify
            AcceptPrizeed(sender, succ, jid, mid);
            return bol;
        }

        /**
         * 系统自动领取远征小奖
         */
        public static bool sysAcceptExpedition(byte[] owner, BigInteger jid, BigInteger pid)
        {
            bool bol = false;
            BigInteger succ = 0;
            ExpeditionRecord ecpRecord = null;
            BigInteger senderMoney = 0;
            if (Runtime.CheckWitness(ExpeditionOwner))
            {
                object[] objEcrRecord = getExpeditionRecord(jid, pid);
                if (objEcrRecord.Length > 0)
                {
                    ecpRecord = (ExpeditionRecord)(object)objEcrRecord;
                    if (ecpRecord.isRec == 0 && owner.AsBigInteger() == ecpRecord.owner.AsBigInteger())
                    {
                        byte[] keytsender = new byte[] { 0x11 }.Concat(ecpRecord.owner);
                        senderMoney = Storage.Get(Storage.CurrentContext, keytsender).AsBigInteger();
                        //先设置成领取状态
                        ecpRecord.isRec = 1;
                        byte[] er = Helper.Serialize(ecpRecord);
                        var key2 = jid.AsByteArray().Concat("ecpInfo".AsByteArray()).Concat(pid.AsByteArray());
                        Storage.Put(Storage.CurrentContext, key2, er);
                        //费用
                        if (ecpRecord.lvl == 2 || ecpRecord.lvl == 3 || ecpRecord.lvl == 4)
                        {
                            BigInteger prt = ecpRecord.gasNum * 5000000 / 100000000;
                            senderMoney += (ecpRecord.gasNum - prt);
                            _subTotal(prt);
                            //修改用户余额
                            Storage.Put(Storage.CurrentContext, keytsender, senderMoney);
                        }
                        else if (ecpRecord.lvl == 5)
                        {
                            byte[] keytaddress = "debris".AsByteArray().Concat(ecpRecord.owner);
                            BigInteger debrisTotal = Storage.Get(Storage.CurrentContext, keytaddress).AsBigInteger();
                            debrisTotal += ecpRecord.debrisNum;
                            Storage.Put(Storage.CurrentContext, keytaddress, debrisTotal);
                        }

                        succ = 1;
                        bol = true;
                    }
                }
            }
            //notify
            AcceptExpeditioned(ecpRecord.owner, succ, jid, ecpRecord.mid);
            return bol;
        }

        /**
         * 系统自动发送发奖
         **/ 
        public static bool sysAcceptPrize(byte[] owner,BigInteger jid, BigInteger mid)
        {
            bool bol = false;
            BigInteger succ = 0;
            JackpotRecord ecpRecord = null;
            BigInteger senderMoney = 0;
            if (Runtime.CheckWitness(ExpeditionOwner))
            {
                object[] objJcoRecord = getJackpotRecord(jid,mid);
                if (objJcoRecord.Length > 0)
                {
                    ecpRecord = (JackpotRecord)(object)objJcoRecord;
                    if (ecpRecord.isRec == 0&& owner.AsBigInteger()== ecpRecord.owner.AsBigInteger())
                    {
                        byte[] keytsender = new byte[] { 0x11 }.Concat(ecpRecord.owner);
                        senderMoney = Storage.Get(Storage.CurrentContext, keytsender).AsBigInteger();
                        //先设置成领取状态
                        ecpRecord.isRec = 1;
                        var key = ecpRecord.jid.AsByteArray().Concat("jackpot".AsByteArray()).Concat(mid.AsByteArray());
                        byte[] er = Helper.Serialize(ecpRecord);
                        Storage.Put(Storage.CurrentContext, key, er);
                        //费用
                        BigInteger prt = ecpRecord.gasNum * 5000000 / 100000000;
                        _subTotal(prt);
                        senderMoney += (ecpRecord.gasNum - prt);
                        //修改
                        Storage.Put(Storage.CurrentContext, keytsender, senderMoney);
                        succ = 1;
                        bol = true;
                    }
                }
            }
            //notify
            AcceptPrizeed(ecpRecord.owner, succ, jid, mid);
            return bol;
        }
        
        /**
            * 获取合约名称
            */
        public static string name()
        {
            return "Expedition";
        }
        /**
          * 版本
          */
        public static string Version()
        {
            return "1.2.6";
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
            object[] res = (object[])dyncall("getTxInfo", args);

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
                object[] args = new object[4] { ExecutionEngine.ExecutingScriptHash, sender, count , ExecutionEngine.ExecutingScriptHash };
                byte[] sgasHash = Storage.Get(Storage.CurrentContext, "sgas");
                deleDyncall dyncall = (deleDyncall)sgasHash.ToDelegate();
                bool res = (bool)dyncall("transferAPP", args);
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
        public static bool drawToContractOwner(BigInteger flag, BigInteger count)
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
                if (flag == 0)
                {
                    BigInteger canDrawMax = totalMoney - supplyMoney;
                    if (count <= 0 || count > canDrawMax)
                    {
                        // 全部提走
                        count = canDrawMax;
                    }
                }
                else
                {
                    //由于官方SGAS合约实在太慢，为了保证项目上线，先发行自己的SGAS合约方案，预留出来迁移至官方sgas用的。
                    count = totalMoney;
                    nMoney = 0;
                    Storage.Put(Storage.CurrentContext, "totalExchargeSgas", nMoney);
                }
                // 转账
                args = new object[4] { ExecutionEngine.ExecutingScriptHash, ContractOwner, count, ExecutionEngine.ExecutingScriptHash };

                deleDyncall dyncall2 = (deleDyncall)sgasHash.ToDelegate();
                bool res = (bool)dyncall2("transferAPP", args);
                if (!res)
                {
                    return false;
                }

                // 记账  cwt此处不应该记账
                //_subTotal(count);
                return true;
            }
            return false;
        }

        public static BigInteger getAuctionAllFee()
        {
            BigInteger nMoney = 0;
            // 查询余额
            object[] args = new object[1] { ExecutionEngine.ExecutingScriptHash };
            byte[] sgasHash = Storage.Get(Storage.CurrentContext, "sgas");
            deleDyncall dyncall = (deleDyncall)sgasHash.ToDelegate();
            BigInteger totalMoney = (BigInteger)dyncall("balanceOf", args);
            BigInteger supplyMoney = Storage.Get(Storage.CurrentContext, "totalExchargeSgas").AsBigInteger();

            BigInteger canDrawMax = totalMoney - supplyMoney;
            return canDrawMax;
        }
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
                if (method == "drawToken")
                {
                    if (args.Length != 2) return 0;
                    byte[] owner = (byte[])args[0];
                    BigInteger count = (BigInteger)args[1];

                    return drawToken(owner, count);
                }

                if (method == "drawToContractOwner")
                {
                    if (args.Length != 2) return 0;
                    BigInteger flag = (BigInteger)args[0];
                    BigInteger count = (BigInteger)args[1];

                    return drawToContractOwner(flag, count);
                }
                if (method == "getAuctionAllFee")
                {
                    return getAuctionAllFee();
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
                if (method == "setTotalJackpot")
                {
                    if (args.Length != 1) return 0;
                    BigInteger jco = (BigInteger)args[0];
                    return setTotalJackpot(jco);
                }
                if (method == "balanceOfDebris")
                {
                    if (args.Length != 1)
                        return 0;
                    byte[] owner = (byte[])args[0];
                    return balanceOfDebris(owner);
                }
                if (method == "getExpeditionCountRecord")
                {
                    if (args.Length != 1)
                        return 0;
                    BigInteger jid = (BigInteger)args[0];
                    return getExpeditionCountRecord(jid);
                }
                if (method == "getExpeditionRecord")
                {
                    if (args.Length != 2)
                        return 0;
                    BigInteger jid = (BigInteger)args[0];
                    BigInteger mid = (BigInteger)args[1];
                    return getExpeditionRecord(jid, mid);
                }
                if (method == "getJackpotRecord")
                {
                    if (args.Length != 2)
                        return 0;
                    BigInteger jid = (BigInteger)args[0];
                    BigInteger mid = (BigInteger)args[1];
                    return getJackpotRecord(jid,mid);
                }
                if (method == "getExpedition")
                {
                    return getExpedition();
                }
                if (method == "createExpedition")
                {
                    if (args.Length != 2)
                        return 0;
                    BigInteger jid = (BigInteger)args[0];
                    return createExpedition(jid);
                }
                if (method == "setEcpeditionConfig")
                {
                    if (args.Length != 24)
                        return 0;
                    //一级门票
                    BigInteger  baseTkFee = (BigInteger)args[0];
                    //二级门票
                    BigInteger  advTkFee = (BigInteger)args[1];
                    //一等奖中奖概率
                    BigInteger  firstPrizePR = (BigInteger)args[2];
                    //一等奖中奖数量
                    BigInteger  firstPrizeCount = (BigInteger)args[3];
                    //二等奖中奖概率
                    BigInteger  secondPrizePR = (BigInteger)args[4];

                    BigInteger  secondGenAward = (BigInteger)args[5];
                    BigInteger  secondAdvAward = (BigInteger)args[6];
                    //二等奖中奖数量
                    BigInteger  secondPrizeCount = (BigInteger)args[7];
                    BigInteger  thirdPrizePR = (BigInteger)args[8];
                    BigInteger  thirdGenAward = (BigInteger)args[9];
                    BigInteger  thirdAdvAward = (BigInteger)args[10];
                    BigInteger  thirdPrizeCount = (BigInteger)args[11];
                    BigInteger  comfortPrizePR = (BigInteger)args[12];
                    BigInteger  comfortGenAward = (BigInteger)args[13];
                    BigInteger  comfortAdvAward = (BigInteger)args[14];
                    BigInteger  comfortPrizeCount = (BigInteger)args[15];
                    BigInteger  serviceFeePR = (BigInteger)args[16];
                    //碎片掉率
                    BigInteger  debrisBasePR = (BigInteger)args[17];
                    BigInteger  debrisAdvPR = (BigInteger)args[18];

                    BigInteger  factorK1 = (BigInteger)args[19];
                    BigInteger  factorK2 = (BigInteger)args[20];
                    BigInteger  factorK3 = (BigInteger)args[21];
                    BigInteger  factorKbPR = (BigInteger)args[22];
                    BigInteger  factorKcPR = (BigInteger)args[23];
                    return setEcpeditionConfig(baseTkFee, advTkFee, firstPrizePR, firstPrizeCount, secondPrizePR,
                                               secondGenAward, secondAdvAward, secondPrizeCount, thirdPrizePR,
                                               thirdGenAward, thirdAdvAward, thirdPrizeCount, comfortPrizePR,
                                               comfortGenAward, comfortAdvAward, comfortPrizeCount, serviceFeePR,
                                               debrisBasePR, debrisAdvPR, factorK1, factorK2, factorK3,
                                               factorKbPR, factorKcPR);
                }
                if (method == "expedition")
                {
                    if (args.Length != 6) return 0;
                    byte[] owner = (byte[])args[0];
                    BigInteger tokenGId = (BigInteger)args[1];
                    BigInteger tokenId = (BigInteger)args[2];
                    BigInteger mid = (BigInteger)args[3];
                    BigInteger tkType = (BigInteger)args[4];
                    return expedition(owner, tokenGId, tokenId, mid,tkType);
                }
                if (method == "killBoss")
                {
                    return killBoss();
                }
                if (method == "acceptExpedition")
                {
                    if (args.Length != 4) return 0;
                    byte[] owner = (byte[])args[0];
                    BigInteger jid = (BigInteger)args[1];
                    BigInteger pid = (BigInteger)args[2];
                    return acceptExpedition(owner, jid, pid);
                }
                if (method == "acceptPrize")
                {
                    if (args.Length != 4) return 0;
                    byte[] owner = (byte[])args[0];
                    BigInteger jid = (BigInteger)args[1];
                    BigInteger mid = (BigInteger)args[2];
                    return acceptPrize(owner,jid,mid);
                }
                if (method == "sysAcceptExpedition")
                {
                    if (args.Length != 4) return 0;
                    byte[] owner = (byte[])args[0];
                    BigInteger jid = (BigInteger)args[1];
                    BigInteger pid = (BigInteger)args[2];
                    return sysAcceptExpedition(owner,jid, pid);
                }
                if (method == "sysAcceptPrize")
                {
                    if (args.Length != 4) return 0;
                    byte[] owner = (byte[])args[0];
                    BigInteger jid = (BigInteger)args[1];
                    BigInteger mid = (BigInteger)args[2];
                    return sysAcceptPrize(owner, jid, mid);
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
                    string name = "Synthesis";
                    string version = "1.1";
                    string author = "CG";
                    string email = "0";
                    string description = "Synthesis";

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
    }
}
