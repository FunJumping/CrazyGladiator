using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using Helper = Neo.SmartContract.Framework.Helper;

using System;
using System.ComponentModel;
using System.Numerics;


namespace NFTContract
{
    /**
     * smart contract for Gladiator
     * @author Clyde
     */
    public class NFT : SmartContract
    {
        // global storage
        // "totalSupply" :  total count of NFT minted
        // "tokenURI" : URI base of NFT // optional

        // tokens :         map<tokenid:biginteger, info:NFTInfo>           // NFT ID to NFTInfo, key = tokenid
        // approve :        map<tokenid:biginteger, address:hash160>        // NFT ID to address which the token is approved, key = "apr"+tokenid

        // use byte\ushort\uint\ulong as index depends on total NFT amount
        // index :          map<index:uint, tokenid:biginteger>             // NFT index to NFT ID, key = "idx"+index, if tokenid is same to index, this can be ignore, optional
        // owner index :    map<address:hash160, tokens:uint[]>             // Owner Address to tokens index array, key = "own"+address, optional
        // "auction": addr          // the auction addr
        // "attrConfig" : AttrConfig // the config of attr

        // extra data:      map<extradatakey:string, data:byte[]>           // NFT ID + datakey to extra data, key = "ex"+tokenid+datakey, optional
        // broker :         map<owner:hash160, broker:hash160>              // Owner Address to Broker Address which is approved, optional

        /**
         * 角斗士属性结构数据
         */
        [Serializable]
        public class NFTInfo
        {
            
            public byte[] owner; // 角斗士拥有者

            public int isGestating; //bool 是否怀孕
            public int isReady; //bool 是否可以怀孕（cd中不可怀）
            public BigInteger cooldownIndex; //uint256  基础cd	（父母受孕时同时触发，父方cd到了才能再次做父亲或母亲；母亲方cd到了才能生小孩，生完立即可以再次做父亲或母亲）
            public BigInteger nextActionAt; //uint256  	下一次可怀或出生时间
            public BigInteger cloneWithId;//uint256		如果已怀孕，则为老公id，否则=0
            public BigInteger birthTime; //uint256			出生时间，时间戳
            public BigInteger matronId; //uint256			母亲id，初代为0
            public BigInteger sireId; //uint256				父亲id，初代为0
            public BigInteger generation; //uint256		世代

            public byte strength; // 力量
            public byte power; // 体力
            public byte agile; // 敏捷
            public byte speed; // 速度

            // 技能ID
            public byte skill1;
            public byte skill2;
            public byte skill3;
            public byte skill4;
            public byte skill5;

            // 装备ID
            public byte equip1;
            public byte equip2;
            public byte equip3;
            public byte equip4;

            public byte restrictAttribute; // 元素属性
            public byte character; // 角色模型资源id

            // 外观属性
            public byte part1;
            public byte part2;
            public byte part3;
            public byte part4;
            public byte part5;
            public byte appear1;
            public byte appear2;
            public byte appear3;
            public byte appear4;

            // 外观属性，游戏中暂未使用
            public byte appear5;
            public byte chest;
            public byte bracer;
            public byte shoulder;
            public byte face;
            public byte lip;
            public byte nose;
            public byte eyes;
            public byte hair;

        }

        // 技能和武器配置
        public class AttrConfig
        {
            // 普通技能
            public BigInteger normalSkillIdMin; // id最小值
            public BigInteger normalSkillIdMax; // id最大值

            // 稀有技能
            public BigInteger rareSkillIdMin;
            public BigInteger rareSkillIdMax;

            // 普通装备
            public BigInteger normalEquipIdMin;
            public BigInteger normalEquipIdMax;

            // 稀有装备
            public BigInteger rareEquipIdMax;
            public BigInteger rareEquipIdMin;

            // 外观属性最大值
            public BigInteger atr1Max;
            public BigInteger atr2Max;
            public BigInteger atr3Max;
            public BigInteger atr4Max;
            public BigInteger atr5Max;
            public BigInteger atr6Max;
            public BigInteger atr7Max;
            public BigInteger atr8Max;
            public BigInteger atr9Max;
            //
        }

        /**
         * 角斗士交易记录
         */
        public class TransferInfo
        {
            public byte[] from;
            public byte[] to;
            public BigInteger value;
        }

        // notify 转账通知
        public delegate void deleTransfer(byte[] from, byte[] to, BigInteger value);
        [DisplayName("transfer")]
        public static event deleTransfer Transferred;

        // notify 授权通知，暂未实现
        public delegate void deleApproved(byte[] owner, byte[] approved, BigInteger tokenId);
        [DisplayName("approve")]
        public static event deleApproved Approved;

        // notify 新的角斗士出生通知
        public delegate void deleBirth(BigInteger tokenId, byte[] owner, int isGestating, int isReady, BigInteger cooldownIndex, BigInteger nextActionAt,
            BigInteger cloneWithId, BigInteger birthTime, BigInteger matronId, BigInteger sireId, BigInteger generation,
            byte strength, byte power, byte agile, byte speed,
            byte skill1, byte skill2, byte skill3, byte skill4, byte skill5, byte equip1, byte equip2, byte equip3, byte equip4,
            byte restrictAttribute, byte character,
            byte part1, byte part2, byte part3, byte part4, byte part5, byte appear1, byte appear2, byte appear3, byte appear4, byte appear5,
            byte chest, byte bracer, byte shoulder,
            byte face, byte lip, byte nose, byte eyes, byte hair);
        [DisplayName("birth")]
        public static event deleBirth Birthed;

        // notify 角斗士克隆通知
        public delegate void deleGladiatorCloned(byte[] owner, BigInteger motherId, BigInteger motherCd, BigInteger fatherId, BigInteger fatherCd);
        [DisplayName("gladiatorCloned")]
        public static event deleGladiatorCloned GladiatorCloned;

        // 合约拥有者，超级管理员
        public static readonly byte[] ContractOwner = "AcKA1A3TRx6ubNzi3Dz2QFW6V9uEkeVasg".ToScriptHash();
        // 有权限发布0代角斗士的钱包地址
        public static readonly byte[] MintOwner = "AcKA1A3TRx6ubNzi3Dz2QFW6V9uEkeVasg".ToScriptHash();

        // 名称
        public static string Name() => "CrazyGladiator";
        // 符号
        public static string Symbol() => "CGL";

        // 存储已发行的key
        private const string KEY_TOTAL = "totalSupply";
        // 发行总量的key
        private const string KEY_ALL = "allSupply";
        //发行总量
        private const ulong ALL_SUPPLY_CG = 4320;
        //版本
        public static string Version() => "1.0.16";

        /**
         * 获取角斗士拥有者
         */
        public static byte[] ownerOf(BigInteger tokenId)
        {
            object[] objInfo = _getNFTInfo(tokenId.AsByteArray());
            NFTInfo info = (NFTInfo)(object) objInfo;
            if (info.owner.Length>0)
            {
                return info.owner;
            }
            else
            {
                //return System.Text.Encoding.ASCII.GetBytes("token does not exist");
                return new byte[] { };
            }
        }

        /**
          * 已经发行的角斗士总数
          */
        public static BigInteger totalSupply()
        {
            return Storage.Get(Storage.CurrentContext, KEY_TOTAL).AsBigInteger();
        }

        /**
          * 发行的角斗士总数
          */
        public static BigInteger allSupply()
        {
            return Storage.Get(Storage.CurrentContext, KEY_ALL).AsBigInteger();
        }

        /**
         * uri
         */
        public static string tokenURI(BigInteger tokenId)
        {
            return "uri/" + tokenId;
        }

        /**
         * 发行促销角斗士
         */
        public static BigInteger mintToken(byte[] tokenOwner, byte strength, byte power, byte agile, byte speed,
            byte skill1, byte skill2, byte skill3, byte skill4, byte skill5, byte equip1, byte equip2, byte equip3, byte equip4,
            byte restrictAttribute, byte character,
            byte part1, byte part2, byte part3, byte part4, byte part5, byte appear5, byte appear6, byte appear7, byte appear8, byte appear9,
            byte chest, byte bracer, byte shoulder,
            byte face, byte lip, byte nose, byte eyes, byte hair)
        {
            return createGladiator(tokenOwner, strength, power, agile, speed,
                skill1, skill2, skill3, skill4, skill5, equip1, equip2, equip3, equip4,
                restrictAttribute, character,
                part1, part2, part3, part4, part5, appear5, appear6, appear7, appear8, appear9,
                chest, bracer, shoulder,
                face, lip, nose, eyes, hair);
        }

        /**
         * 生成新的角斗士数据，并记录
         */
        private static BigInteger createGladiator(byte[] tokenOwner, byte strength, byte power, byte agile, byte speed,
            byte skill1, byte skill2, byte skill3, byte skill4, byte skill5, byte equip1, byte equip2, byte equip3, byte equip4,
            byte restrictAttribute, byte character,
            byte part1, byte part2, byte part3, byte part4, byte part5, byte appear1, byte appear2, byte appear3, byte appear4, byte appear5,
            byte chest, byte bracer, byte shoulder,
            byte face, byte lip, byte nose, byte eyes, byte hair)
        {
            if (tokenOwner.Length != 20)
            {
                // Owner error.
                return 0;
            }
           
            //
            if (Runtime.CheckWitness(MintOwner))
            {
                //判断下是否超过总量
                byte[] tokenaId = Storage.Get(Storage.CurrentContext, KEY_ALL);
                byte[] tokenId = Storage.Get(Storage.CurrentContext, KEY_TOTAL);
                if (tokenId.AsBigInteger()>= tokenaId.AsBigInteger())
                {
                    return 0;
                }
                BigInteger newToken = tokenId.AsBigInteger() + 1;
                tokenId = newToken.AsByteArray();

                NFTInfo newInfo = new NFTInfo();
                newInfo.owner = tokenOwner;
                newInfo.isGestating = 0;
                newInfo.isReady = 0;
                newInfo.cooldownIndex = 0;
                newInfo.nextActionAt = 0;
                newInfo.cloneWithId = 0;
                newInfo.birthTime = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
                newInfo.matronId = 0;
                newInfo.sireId = 0;
                newInfo.generation = 0;

                newInfo.strength = strength;
                newInfo.power = power;
                newInfo.agile = agile;
                newInfo.speed = speed;

                newInfo.skill1 = skill1;
                newInfo.skill2 = skill2;
                newInfo.skill3 = skill3;
                newInfo.skill4 = skill4;
                newInfo.skill5 = skill5;

                newInfo.equip1 = equip1;
                newInfo.equip2 = equip2;
                newInfo.equip3 = equip3;
                newInfo.equip4 = equip4;

                newInfo.restrictAttribute = restrictAttribute;
                newInfo.character = character;
                newInfo.part1 = part1;
                newInfo.part2 = part2;
                newInfo.part3 = part3;
                newInfo.part4 = part4;
                newInfo.part5 = part5;
                newInfo.appear1 = appear1;
                newInfo.appear2 = appear2;
                newInfo.appear3 = appear3;
                newInfo.appear4 = appear4;
                newInfo.appear5 = appear5;
                newInfo.chest = chest;
                newInfo.bracer = bracer;
                newInfo.shoulder = shoulder;
                newInfo.face = face;
                newInfo.lip = lip;
                newInfo.nose = nose;
                newInfo.eyes = eyes;
                newInfo.hair = hair;

                _putNFTInfo(tokenId, newInfo);
                //_addOwnerToken(tokenOwner, tokenId.AsBigInteger());

                Storage.Put(Storage.CurrentContext, KEY_TOTAL, tokenId);

                // notify
                Birthed(tokenId.AsBigInteger(), newInfo.owner, newInfo.isGestating, newInfo.isReady, newInfo.cooldownIndex, newInfo.nextActionAt, 
                    newInfo.cloneWithId, newInfo.birthTime, newInfo.matronId, newInfo.sireId, newInfo.generation,
                    newInfo.strength, newInfo.power, newInfo.agile, newInfo.speed,
                    newInfo.skill1, newInfo.skill2, newInfo.skill3, newInfo.skill4, newInfo.skill5,
                    newInfo.equip1, newInfo.equip2, newInfo.equip3, newInfo.equip4,
                    newInfo.restrictAttribute, newInfo.character,
                    newInfo.part1, newInfo.part2, newInfo.part3, newInfo.part4, newInfo.part5,
                    newInfo.appear1, newInfo.appear2, newInfo.appear3, newInfo.appear4, newInfo.appear5, newInfo.chest, newInfo.bracer, newInfo.shoulder,
                    newInfo.face, newInfo.lip, newInfo.nose, newInfo.eyes, newInfo.hair);
                return tokenId.AsBigInteger();
            }
            else
            {
                Runtime.Log("Only the contract owner may mint new tokens.");
                return 0;
            }
        }

        /**
         * 和某个角斗士进行克隆，供其他合约调用
         */
        public static bool breedWithId_app(byte[] sender, BigInteger mother, BigInteger father)
        {
            //if (!Runtime.CheckWitness(sender))
            //{
            //    //没有签名
            //    return false;
            //}
            if (!canBreedWithById(mother, father))
            {
                return false;
            }
            object[] objMotherInfo = _getNFTInfo(mother.AsByteArray());
            object[] objFatherInfo = _getNFTInfo(father.AsByteArray());
            NFTInfo motherInfo;
            if (objMotherInfo.Length > 0 && objFatherInfo.Length > 0)
            {
                motherInfo = (NFTInfo)(object)objMotherInfo;
                byte[] owner = motherInfo.owner;

                if (owner.AsBigInteger() == sender.AsBigInteger())
                {
                    _breedWith(mother, father);                 
                }
            }
            return false;
        }

        /**
         * 和某个角斗士进行克隆
         */
        private static bool _breedWith(BigInteger mother, BigInteger father)
        {
            object[] objMotherInfo = _getNFTInfo(mother.AsByteArray());
            object[] objFatherInfo = _getNFTInfo(father.AsByteArray());
            NFTInfo motherInfo;
            NFTInfo fatherInfo;

            var nowtime = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;

            motherInfo = (NFTInfo)(object)objMotherInfo;
            fatherInfo = (NFTInfo)(object)objFatherInfo;

            BigInteger coolTime = _getCoolTime(motherInfo.cooldownIndex);
            motherInfo.isGestating = 1;
            motherInfo.cloneWithId = father;
            motherInfo.nextActionAt = nowtime + coolTime;
            //先注释掉,改成在角斗士出生的时候再计算
            //motherInfo.cooldownIndex += 1;

            _putNFTInfo(mother.AsByteArray(), motherInfo);

            //
            coolTime = _getCoolTime(fatherInfo.cooldownIndex);
            fatherInfo.nextActionAt = nowtime + coolTime;
            //先注释掉,改成在角斗士出生的时候再计算
            //fatherInfo.cooldownIndex += 1;

            _putNFTInfo(father.AsByteArray(), fatherInfo);

            // notify
            GladiatorCloned(motherInfo.owner, mother, motherInfo.nextActionAt, father, fatherInfo.nextActionAt);
            return true;
        }

        /**
         * 角斗士出生
         */
        public static BigInteger giveBirth(BigInteger motherId)
        {
            object[] objMotherInfo = _getNFTInfo(motherId.AsByteArray());
            NFTInfo motherInfo;
            NFTInfo fatherInfo;
            if (objMotherInfo.Length > 0)
            {
                motherInfo = (NFTInfo)(object)objMotherInfo;

                BigInteger fatherId = motherInfo.cloneWithId;
                var nowtime = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
                if(fatherId<=0 || nowtime<motherInfo.nextActionAt)
                {
                    // 没怀孕或不到出生时间
                    return 0;
                }

                object[] objFatherInfo = _getNFTInfo(fatherId.AsByteArray());
                if (objFatherInfo.Length > 0)
                {
                    fatherInfo = (NFTInfo)(object)objFatherInfo;

                    // get child gene
                    object[] objInfo = _getMixGene(motherInfo, fatherInfo);
                    if(objInfo.Length == 0)
                    {
                        return 0;
                    }
                    NFTInfo newInfo = (NFTInfo)(object)objInfo;
                    newInfo.sireId = fatherId;
                    newInfo.matronId = motherId;

                    byte[] tokenId = Storage.Get(Storage.CurrentContext, KEY_TOTAL);
                    BigInteger newToken = tokenId.AsBigInteger() + 1;
                    tokenId = newToken.AsByteArray();

                    _putNFTInfo(tokenId, newInfo);

                    //_addOwnerToken(motherInfo.owner, tokenId.AsBigInteger());

                    Storage.Put(Storage.CurrentContext, KEY_TOTAL, tokenId);

                    //
                    motherInfo.cloneWithId = 0;
                    motherInfo.nextActionAt = 0;
                    motherInfo.isGestating = 0;
                    //判断克隆CD是否升级
                    BigInteger isUp = motherInfo.cooldownIndex % 2;
                    if (isUp==0&&motherInfo.cooldownIndex>0)
                    {
                        motherInfo.cooldownIndex += 1;
                    }
                    _putNFTInfo(motherId.AsByteArray(), motherInfo);

                    fatherInfo.nextActionAt = 0;
                    //判断克隆CD是否升级
                    isUp = motherInfo.cooldownIndex % 2;
                    if (isUp == 0 && fatherInfo.cooldownIndex > 0)
                    {
                        fatherInfo.cooldownIndex += 1;
                    }
                    _putNFTInfo(fatherId.AsByteArray(), fatherInfo);

                    // notify
                    Birthed(tokenId.AsBigInteger(), newInfo.owner, newInfo.isGestating, newInfo.isReady, newInfo.cooldownIndex, newInfo.nextActionAt,
                        newInfo.cloneWithId, newInfo.birthTime, newInfo.matronId, newInfo.sireId, newInfo.generation,
                        newInfo.strength, newInfo.power, newInfo.agile, newInfo.speed,
                        newInfo.skill1, newInfo.skill2, newInfo.skill3, newInfo.skill4, newInfo.skill5,
                        newInfo.equip1, newInfo.equip2, newInfo.equip3, newInfo.equip4,
                        newInfo.restrictAttribute, newInfo.character,
                        newInfo.part1, newInfo.part2, newInfo.part3, newInfo.part4, newInfo.part5,
                        newInfo.appear1, newInfo.appear2, newInfo.appear3, newInfo.appear4, newInfo.appear5, newInfo.chest, newInfo.bracer, newInfo.shoulder,
                        newInfo.face, newInfo.lip, newInfo.nose, newInfo.eyes, newInfo.hair);

                    return 1;
                }
            }
            return 0;
        }

        /**
         * 计算两个角斗士进行克隆操作后，可以继承的稀有技能和稀有装备
         */
        private static BigInteger[] getExtendsAttr(NFTInfo motherInfo, NFTInfo fatherInfo, BigInteger rand, AttrConfig attrConfig)
        {
            BigInteger exSkillNum = 0;
            BigInteger exEquipNum = 0;
            BigInteger[] ret = new BigInteger[15];
            int curIndex = 2;

            rand = (randA * rand + randB) % randM;

            NFTInfo extInfo;
            if (rand % 2 == 0)
            {
                extInfo = motherInfo;
            }
            else
            {
                extInfo = fatherInfo;
            }

            BigInteger skill1 = extInfo.skill1;
            if (skill1 >= attrConfig.rareSkillIdMin)
            {
                rand = (randA * rand + randB) % randM;
                if (rand % 10 < 8)
                {
                    exSkillNum +=1;

                    curIndex++;
                    ret[curIndex] = skill1;
                }
            }

            skill1 = extInfo.skill2;
            if (skill1 >= attrConfig.rareSkillIdMin)
            {
                rand = (randA * rand + randB) % randM;
                if (rand % 10 < 8)
                {
                    exSkillNum += 1;

                    curIndex++;
                    ret[curIndex] = skill1;
                }
            }

            skill1 = extInfo.skill3;
            if (skill1 >= attrConfig.rareSkillIdMin)
            {
                rand = (randA * rand + randB) % randM;
                if (rand % 10 < 8)
                {
                    exSkillNum += 1;

                    curIndex++;
                    ret[curIndex] = skill1;
                }
            }

            skill1 = extInfo.skill4;
            if (skill1 >= attrConfig.rareSkillIdMin)
            {
                rand = (randA * rand + randB) % randM;
                if (rand % 10 < 8)
                {
                    exSkillNum += 1;

                    curIndex++;
                    ret[curIndex] = skill1;
                }
            }

            skill1 = extInfo.skill5;
            if (skill1 >= attrConfig.rareSkillIdMin)
            {
                rand = (randA * rand + randB) % randM;
                if (rand % 10 < 8)
                {
                    exSkillNum += 1;

                    curIndex++;
                    ret[curIndex] = skill1;
                }
            }

            // equip
            var equip1 = extInfo.equip1;
            if (equip1 >= attrConfig.rareEquipIdMin)
            {
                rand = (randA * rand + randB) % randM;
                if (rand % 10 < 8)
                {
                    exEquipNum += 1;

                    curIndex++;
                    ret[curIndex] = equip1;
                }
            }

            equip1 = extInfo.equip2;
            if (equip1 >= attrConfig.rareEquipIdMin)
            {
                rand = (randA * rand + randB) % randM;
                if (rand % 10 < 8)
                {
                    exEquipNum += 1;

                    curIndex++;
                    ret[curIndex] = equip1;
                }
            }

            equip1 = extInfo.equip3;
            if (equip1 >= attrConfig.rareEquipIdMin)
            {
                rand = (randA * rand + randB) % randM;
                if (rand % 10 < 8)
                {
                    exEquipNum += 1;

                    curIndex++;
                    ret[curIndex] = equip1;
                }
            }

            equip1 = extInfo.equip4;
            if (equip1 >= attrConfig.rareEquipIdMin)
            {
                rand = (randA * rand + randB) % randM;
                if (rand % 10 < 8)
                {
                    exEquipNum += 1;

                    curIndex++;
                    ret[curIndex] = equip1;
                }
            }

            ret[0] = rand;
            ret[1] = exSkillNum;
            ret[2] = exEquipNum;
            return ret;
        }

        // 随机数参数
        private const int MAXN = 1 << 20;
        private const ulong randM = MAXN;
        private const ulong randA = 9;  // a = 4p + 1
        private const ulong randB = 7;  // b = 2q + 1

        /**
         * 基因混合算法
         */
        private static object[] _getMixGene(NFTInfo motherInfo, NFTInfo fatherInfo)
        {
            // 为了增加游戏趣味性，基因混合的代码没有公开，需要自行根据游戏方案进行补全
            // 这里为了能顺利编译，返回了一个没有初始数据的对象
            NFTInfo newInfo = new NFTInfo();
            return (object[])(object)newInfo;
            
        }

        /**
         * 怀孕的角斗士是否到了生小孩的时间
         */
        public static bool isReadyToBreed(BigInteger tokenId)
        {
            object[] objInfo = _getNFTInfo(tokenId.AsByteArray());
            if (objInfo.Length > 0)
            {
                NFTInfo nftInfo = (NFTInfo)(object)objInfo;
                if(nftInfo.cloneWithId <= 0)
                {
                    return false;
                }

                var nowtime = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
                if(nftInfo.nextActionAt > 0 && nowtime >= nftInfo.nextActionAt)
                {
                    return true;
                }
            }
            return false;
        }

        /**
         * 判断角斗士是否怀孕
         */
        public static bool isPregnant(BigInteger tokenId)
        {
            object[] objInfo = _getNFTInfo(tokenId.AsByteArray());
            if (objInfo.Length > 0)
            {
                NFTInfo nftInfo = (NFTInfo)(object)objInfo;
                if (nftInfo.cloneWithId > 0)
                {
                    return true;
                }
            }
            return false;
        }

        /**
         * 判断两个角斗士是否可以进行克隆操作
         */
        public static bool canBreedWithById(BigInteger mother, BigInteger father)
        {
            object[] objMotherInfo = _getNFTInfo(mother.AsByteArray());
            object[] objFatherInfo = _getNFTInfo(father.AsByteArray());
            NFTInfo motherInfo;
            NFTInfo fatherInfo;
            if (objMotherInfo.Length > 0 && objFatherInfo.Length > 0)
            {
                motherInfo = (NFTInfo)(object)objMotherInfo;
                fatherInfo = (NFTInfo)(object)objFatherInfo;

                if (motherInfo.cloneWithId > 0 || fatherInfo.cloneWithId > 0)
                {
                    return false;
                }

                var nowtime = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;

                if ((motherInfo.nextActionAt== 0 || (motherInfo.cloneWithId==0 && nowtime>motherInfo.nextActionAt)) &&
                    (fatherInfo.nextActionAt==0 || (fatherInfo.cloneWithId==0 && nowtime>fatherInfo.nextActionAt)) )
                {
                    return true;
                }
            }
            return false;
        }

        /**
         * 将角斗士资产转账给其他人
         */
        public static bool transfer(byte[] from, byte[] to, BigInteger tokenId)
        {
            if (from.Length != 20|| to.Length != 20)
            {
                return false;
            }

            StorageContext ctx = Storage.CurrentContext;

            if (from == to)
            {
                //Runtime.Log("Transfer to self!");
                return true;
            }

            object[] objInfo = _getNFTInfo(tokenId.AsByteArray());
            if(objInfo.Length == 0)
            {
                return false;
            }

            NFTInfo info = (NFTInfo)(object)objInfo;
            byte[] ownedBy = info.owner;

            if (from != ownedBy)
            {
                //Runtime.Log("Token is not owned by tx sender");
                return false;
            }
            //bool res = _subOwnerToken(from, tokenId);
            //if (!res)
            //{
            //    //Runtime.Log("Unable to transfer token");
            //    return false;
            //}
            //_addOwnerToken(to, tokenId);

            info.owner = to;
            _putNFTInfo(tokenId.AsByteArray(), info);

            //remove any existing approvals for this token
            byte[] approvalKey = "apr/".AsByteArray().Concat(tokenId.AsByteArray());
            Storage.Delete(ctx, approvalKey);

            //记录交易信息
            _setTxInfo(from, to, tokenId);

            Transferred(from, to, tokenId);
            return true;

        }

        /**
         * 进行授权后，将授权人的角斗士资产转账给其他人
         */
        public static bool transferFrom(byte[] tokenFrom, byte[] tokenTo, BigInteger tokenId)
        {
            if (tokenFrom.Length != 20)
            {
                return false;
            }
            if (tokenTo.Length != 20)
            {
                return false;
            }

            if (tokenFrom == tokenTo)
            {
                Runtime.Log("Transfer to self!");
                return true;
            }

            object[] objInfo = _getNFTInfo(tokenId.AsByteArray());
            if (objInfo.Length == 0)
            {
                return false;
            }

            NFTInfo info = (NFTInfo)(object)objInfo;

            byte[] tokenOwner = info.owner;
            if (tokenOwner.Length != 20)
            {
                Runtime.Log("Token does not exist");
                return false;
            }
            if (tokenFrom != tokenOwner)
            {
                Runtime.Log("From address is not the owner of this token");
                return false;
            }

            byte[] approvalKey = "apr/".AsByteArray().Concat(tokenId.AsByteArray());
            byte[] authorizedSpender = Storage.Get(Storage.CurrentContext, approvalKey);

            if (authorizedSpender.Length == 0)
            {
                Runtime.Log("No approval exists for this token");
                return false;
            }

            if (Runtime.CheckWitness(authorizedSpender))
            {
                //bool res = _subOwnerToken(tokenFrom, tokenId);
                //if (res == false)
                //{
                //    Runtime.Log("Unable to transfer token");
                //    return false;
                //}
                //_addOwnerToken(tokenTo, tokenId);

                info.owner = tokenTo;
                _putNFTInfo(tokenId.AsByteArray(), info);

                // remove the approval for this token
                Storage.Delete(Storage.CurrentContext, approvalKey);

                Runtime.Log("Transfer complete");

                //记录交易信息
                _setTxInfo(tokenFrom, tokenTo, tokenId);

                Transferred(tokenFrom, tokenTo, tokenId);
                return true;
            }

            Runtime.Log("Transfer by tx sender not approved by token owner");
            return false;
        }

        /**
         * 获取角斗士信息
         */
        public static NFTInfo tokenData(BigInteger tokenId)
        {
            object[] objInfo = _getNFTInfo(tokenId.AsByteArray());
            NFTInfo info = (NFTInfo)(object)objInfo;
            return info;
        }

        /**
         * 获取总发行量
         */
        public static byte[] getAllSupply()
        {
            return Storage.Get(Storage.CurrentContext, "auction");
        }

        /**
         * 获取拍卖行地址
         */
        public static byte[] getAuctionAddr()
        {
            return Storage.Get(Storage.CurrentContext, "auction");
        }

        /**
         * 设置拍卖行地址（这里增加一个添加角斗士发行总量的操作）
         */
        public static bool setAuctionAddr(byte[] auctionAddr)
        {
            if (Runtime.CheckWitness(ContractOwner))
            {
                Storage.Put(Storage.CurrentContext, "auction", auctionAddr);
                Storage.Put(Storage.CurrentContext, KEY_ALL, ALL_SUPPLY_CG);
                return true;
            }
            return false;
        }

        /**
         * 获取技能武器属性配置参数
         */
        public static object[] getAttrConfig()
        {
            byte[] v = Storage.Get(Storage.CurrentContext, "attrConfig");
            if (v.Length == 0)
            {
                return new object[0];
            }

            return (object[])Helper.Deserialize(v);
        }

        /**
         * 设置技能武器配置参数
         */
        public static bool setAttrConfig(int normalSkillIdMax, int rareSkillIdMax, int normalEquipMax, int rareEquipMax,
            int atr1Max, int atr2Max, int atr3Max, int atr4Max, int atr5Max, int atr6Max, int atr7Max, int atr8Max, int atr9Max)
        {
            if (Runtime.CheckWitness(ContractOwner))
            {
                AttrConfig conf = new AttrConfig();
                conf.normalSkillIdMin = 1;
                conf.rareSkillIdMin = 201;
                conf.normalEquipIdMin = 1;
                conf.rareEquipIdMin = 201;


                conf.normalSkillIdMax = normalSkillIdMax;
                conf.rareSkillIdMax = rareSkillIdMax;
                conf.normalEquipIdMax = normalEquipMax;
                conf.rareEquipIdMax = rareEquipMax;

                conf.atr1Max = atr1Max;
                conf.atr2Max = atr2Max;
                conf.atr3Max = atr3Max;
                conf.atr4Max = atr4Max;
                conf.atr5Max = atr5Max;
                conf.atr6Max = atr6Max;
                conf.atr7Max = atr7Max;
                conf.atr8Max = atr8Max;
                conf.atr9Max = atr9Max;

                byte[] bytesConf = Helper.Serialize(conf);
                Storage.Put(Storage.CurrentContext, "attrConfig", bytesConf);
                return true;
            }
            return false;
        }

        /**
         * 授权给某人操作自己的某个角斗士
         */
        public static bool approve(byte[] approved, BigInteger tokenId)
        {
            if (approved.Length != 20)
            {
                return false;
            }

            object[] objInfo = _getNFTInfo(tokenId.AsByteArray());
            NFTInfo info = (NFTInfo)(object)objInfo;

            byte[] tokenOwner = info.owner;
            if (tokenOwner.Length != 20)
            {
                Runtime.Log("Token does not exist");
                return false;
            }

            if (Runtime.CheckWitness(tokenOwner))
            {
                string approvalKey = "apr/" + tokenId;

                // only one third-party spender can be approved
                // at any given time for a specific token

                Storage.Put(Storage.CurrentContext, approvalKey, approved);
                Approved(tokenOwner, approved, tokenId);

                return true;
            }

            Runtime.Log("Incorrect permission");
            return false;
        }

        /**
         * 合约入口
         */
        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
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
                    byte[] signature = operation.AsByteArray();
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
                if (operation == "version") return Version();
                if (operation == "name") return Name();
                if (operation == "symbol") return Symbol();
                if (operation == "decimals") return 0; // NFT can't divide, decimals allways zero
                if (operation == "totalSupply") return totalSupply();

                if (operation == "hasExtraData") return false;
                if (operation == "isEnumable") return false;
                if (operation == "hasBroker") return false;

                if (operation == "ownerOf")
                {
                    BigInteger tokenId = (BigInteger)args[0];
                    return ownerOf(tokenId);
                }

                if (operation == "transfer")
                {
                    if (args.Length != 3)
                        return false;

                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger tokenId = (BigInteger)args[2];

                    //没有from签名，不让转
                    if (!Runtime.CheckWitness(from))
                    {
                        return false;
                    }
                    //如果有跳板调用，不让转
                    if (ExecutionEngine.EntryScriptHash.AsBigInteger() != callscript.AsBigInteger())
                    {
                        return false;
                    }
                    return transfer(from, to, tokenId);
                }

                if (operation == "transferFrom_app")
                {
                    if (args.Length != 3)
                        return false;

                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger tokenId = (BigInteger)args[2];

                    //没有from签名，不让转
                    if (!Runtime.CheckWitness(from))
                    {
                        return false;
                    }
                    byte[] auctionAddr = Storage.Get(Storage.CurrentContext, "auction");
                    if(callscript.AsBigInteger() != auctionAddr.AsBigInteger())
                    {
                        return false;
                    }
                    return transfer(from, to, tokenId);
                }

                if (operation == "transfer_app")
                {
                    if (args.Length != 3)
                        return false;

                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger tokenId = (BigInteger)args[2];

                    //如果from 不是 传入脚本 不让转
                    if (from.AsBigInteger() != callscript.AsBigInteger())
                        return false;

                    return transfer(from, to, tokenId);
                }

                if (operation == "bidOnClone_app")
                {
                    // 和别人的角斗士克隆
                    if (args.Length != 3) return 0;
                    byte[] owner = (byte[])args[0];
                    BigInteger motherGlaId = (BigInteger)args[1];
                    BigInteger fatherGlaId = (BigInteger)args[2];

                    byte[] auctionAddr = Storage.Get(Storage.CurrentContext, "auction");
                    if (callscript.AsBigInteger() != auctionAddr.AsBigInteger())
                    {
                        return false;
                    }

                    return breedWithId_app(owner, motherGlaId, fatherGlaId);
                }

                if (operation == "createGen0Auction_app")
                {
                    //if (args.Length != 34) return 0;

                    byte[] tokenOwner = (byte[])args[0];
                    byte strength = (byte)args[1];
                    byte power = (byte)args[2];
                    byte agile = (byte)args[3];
                    byte speed = (byte)args[4];

                    byte skill1 = (byte)args[5];
                    byte skill2 = (byte)args[6];
                    byte skill3 = (byte)args[7];
                    byte skill4 = (byte)args[8];
                    byte skill5 = (byte)args[9];

                    byte equip1 = (byte)args[10];
                    byte equip2 = (byte)args[11];
                    byte equip3 = (byte)args[12];
                    byte equip4 = (byte)args[13];

                    byte restrictAttribute = (byte)args[14];
                    byte character = (byte)args[15];
                    byte part1 = (byte)args[16];
                    byte part2 = (byte)args[17];
                    byte part3 = (byte)args[18];
                    byte part4 = (byte)args[19];
                    byte part5 = (byte)args[20];
                    byte nudeC = (byte)args[21];
                    byte shoes = (byte)args[22];
                    byte knees = (byte)args[23];
                    byte pants = (byte)args[24];
                    byte belt = (byte)args[25];
                    byte chest = (byte)args[26];
                    byte bracer = (byte)args[27];
                    byte shoulder = (byte)args[28];
                    byte face = (byte)args[29];
                    byte lip = (byte)args[30];
                    byte nose = (byte)args[31];
                    byte eyes = (byte)args[32];
                    byte hair = (byte)args[33];

                    byte[] auctionAddr = Storage.Get(Storage.CurrentContext, "auction");
                    if (callscript.AsBigInteger() != auctionAddr.AsBigInteger())
                    {
                        return false;
                    }

                    return createGladiator(tokenOwner, strength, power, agile, speed,
                                    skill1, skill2, skill3, skill4, skill5, equip1, equip2, equip3, equip4,
                                    restrictAttribute, character,
                                    part1, part2, part3, part4, part5,
                                    nudeC, shoes, knees, pants, belt, chest, bracer, shoulder,
                                    face, lip, nose, eyes, hair);
                }

                if (operation == "mintToken")
                {
                    if (args.Length != 34) return 0;
                    byte[] owner = (byte[])args[0];
                    byte strength = (byte)args[1];
                    byte power = (byte)args[2];
                    byte agile = (byte)args[3];
                    byte speed = (byte)args[4];

                    byte skill1 = (byte)args[5];
                    byte skill2 = (byte)args[6];
                    byte skill3 = (byte)args[7];
                    byte skill4 = (byte)args[8];
                    byte skill5 = (byte)args[9];

                    byte equip1 = (byte)args[10];
                    byte equip2 = (byte)args[11];
                    byte equip3 = (byte)args[12];
                    byte equip4 = (byte)args[13];

                    byte restrictAttribute = (byte)args[14];
                    byte character = (byte)args[15];

                    byte part1 = (byte)args[16];
                    byte part2 = (byte)args[17];
                    byte part3 = (byte)args[18];
                    byte part4 = (byte)args[19];
                    byte part5 = (byte)args[20];
                    byte nudeC = (byte)args[21];
                    byte shoes = (byte)args[22];
                    byte knees = (byte)args[23];
                    byte pants = (byte)args[24];
                    byte belt = (byte)args[25];
                    byte chest = (byte)args[26];
                    byte bracer = (byte)args[27];
                    byte shoulder = (byte)args[28];
                    byte face = (byte)args[29];
                    byte lip = (byte)args[30];
                    byte nose = (byte)args[31];
                    byte eyes = (byte)args[32];
                    byte hair = (byte)args[33];

                    return mintToken(owner, strength, power, agile, speed,
                        skill1, skill2, skill3, skill4, skill5, equip1, equip2, equip3, equip4,
                        restrictAttribute, character, part1, part2, part3, part4, part5,
                        nudeC, shoes, knees, pants, belt, chest, bracer, shoulder,
                        face, lip, nose, eyes, hair);
                }

                if (operation == "giveBirth")
                {
                    if (args.Length != 1) return 0;
                    BigInteger matronId = (BigInteger)args[0];

                    return giveBirth(matronId);
                }

                if (operation == "approve")
                {
                    byte[] approved = (byte[])args[0];
                    BigInteger tokenId = (BigInteger)args[1];

                    return approve(approved, tokenId);
                }

                if (operation == "tokenData")
                {
                    BigInteger tokenId = (BigInteger)args[0];

                    return tokenData(tokenId);
                }

                if (operation == "getTXInfo")
                {
                    if (args.Length != 1)
                        return 0;
                    byte[] txid = (byte[])args[0];
                    return getTXInfo(txid);
                }

                if (operation == "isReadyToBreed")
                {
                    if (args.Length != 1) return 0;
                    BigInteger glaId = (BigInteger)args[0];
                    return isReadyToBreed(glaId);
                }

                if (operation == "breedWithMy_app")
                {
                    if (args.Length != 3) return 0;
                    byte[] owner = (byte[])args[0];
                    BigInteger motherGlaId = (BigInteger)args[1];
                    BigInteger fatherGlaId = (BigInteger)args[2];

                    byte[] auctionAddr = Storage.Get(Storage.CurrentContext, "auction");
                    if (callscript.AsBigInteger() != auctionAddr.AsBigInteger())
                    {
                        return false;
                    }

                    object[] objFatherInfo = _getNFTInfo(fatherGlaId.AsByteArray());
                    if (objFatherInfo.Length > 0 )
                    {
                        NFTInfo fatherInfo = (NFTInfo)(object)objFatherInfo;
                        if (fatherInfo.owner.AsBigInteger() == owner.AsBigInteger())
                        {
                            return breedWithId_app(owner, motherGlaId, fatherGlaId);
                        }
                    }

                    return false;
                }

                if (operation == "getAuctionAddr")
                {
                    return getAuctionAddr();
                }

                if (operation == "getAllSupply")
                {
                    return getAllSupply();
                }

                if (operation == "isPregnant")
                {
                    if (args.Length != 1) return 0;
                    BigInteger glaId = (BigInteger)args[0];
                    return isPregnant(glaId);
                }
                if (operation == "canBreedWithById")
                {
                    if (args.Length != 2) return 0;
                    BigInteger motherGlaId = (BigInteger)args[0];
                    BigInteger fatherGlaId = (BigInteger)args[1];
                    return canBreedWithById(motherGlaId, fatherGlaId);
                }

                if (operation == "setAuctionAddr")
                {
                    if (args.Length != 1) return 0;
                    byte[] addr = (byte[])args[0];

                    return setAuctionAddr(addr);
                }

                if(operation == "setAttrConfig")
                {
                    if (args.Length != 13) return 0;
                    int normalSkillIdMax = (int)args[0];
                    int rareSkillIdMax = (int)args[1];
                    int normalEquipMax = (int)args[2];
                    int rareEquipMax = (int)args[3];

                    int atr0Max = (int)args[4];
                    int atr1Max = (int)args[5];
                    int atr2Max = (int)args[6];
                    int atr3Max = (int)args[7];
                    int atr4Max = (int)args[8];
                    int atr5Max = (int)args[9];
                    int atr6Max = (int)args[10];
                    int atr7Max = (int)args[11];
                    int atr8Max = (int)args[12];

                    return setAttrConfig(normalSkillIdMax, rareSkillIdMax, normalEquipMax, rareEquipMax,
                        atr0Max, atr1Max, atr2Max, atr3Max, atr4Max, atr5Max, atr6Max, atr7Max, atr8Max);
                }

                if (operation == "getAttrConfig")
                {
                    return getAttrConfig();
                }

                if (operation == "transferFrom")
                {
                    if (args.Length != 3)
                        return false;

                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger tokenId = (BigInteger)args[2];
                    return transferFrom(from, to, tokenId);
                }

                if (operation == "tokenURI")
                {
                    BigInteger tokenId = (BigInteger)args[0];
                    return tokenURI(tokenId);
                }


                //if (operation == "tokenExtraData")
                //{
                //    BigInteger tokenId = (BigInteger)args[0];
                //    string key = (string)args[1];
                //    return TokenExtraData(tokenId, key);
                //}

                if (operation == "balanceOf")
                {
                    if (args.Length != 1) return 0;
                    byte[] owner = (byte[])args[0];
                    return balanceOf(owner);
                }

                if (operation == "tokensOfOwner")
                {
                    byte[] owner = (byte[])args[0];

                    return tokensOfOwner(owner);
                }

                if (operation == "tokenOfOwnerByIndex")
                {
                    byte[] owner = (byte[])args[0];
                    BigInteger index = (BigInteger)args[1];

                    return tokenOfOwnerByIndex(owner, index);
                }

                if (operation == "allowance")
                {
                    BigInteger tokenId = (BigInteger)args[0];

                    return allowance(tokenId);
                }
                if (operation == "upgrade")//合约的升级就是在合约中要添加这段代码来实现
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
                    string name = "NFT";
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
                //if (operation == "tokenByIndex")
                //{
                //    BigInteger index = (BigInteger)args[0];
                //    return TokenByIndex(index);
                //}

                //if (operation == "approveBroker")
                //{
                //    byte[] owner = (byte[])args[0];
                //    byte[] broker = (byte[])args[1];
                //    bool isApproved = (bool)args[2];

                //    return ApproveBroker(owner, broker, isApproved);
                //}

                //if (operation == "brokerOfOwner")
                //{
                //    byte[] owner = (byte[])args[0];

                //    return brokerOfOwner(owner);
                //}

                //if (operation == "modifyURIBase")
                //{
                //    string uriBase = (string)args[0];

                //    return modifyURIBase(uriBase);
                //}

                //if (operation == "modifyExtraData")
                //{
                //    BigInteger tokenId = (BigInteger)args[0];
                //    string key = (string)args[1];
                //    Object extraData = (Object)args[2];

                //    return modifyExtraData(tokenId, key, extraData);
                //}

                //if (operation == "delExtraData")
                //{
                //    BigInteger tokenId = (BigInteger)args[0];
                //    string key = (string)args[1];

                //    return DelExtraData(tokenId, key);
                //}

            }

            return false;
        }

        /**
         * 获取冷却时间，单位:秒
         */
        private static BigInteger _getCoolTime(BigInteger coolIndex)
        {
            BigInteger[] coolData = new BigInteger[19] {
                1, 10, 30, 60, 120, 240,
                489, 960, 1440, 2160, 3130,
                4380, 5900, 7670,9600,11500,13300,14600,15300};
            if(coolIndex>18)
            {
                coolIndex = 18;
            }

            BigInteger coolTime = coolData[(int)coolIndex];
            coolTime *= 60;
            return coolTime;
        }

        /**
         * 获取角斗士结构
         */
        private static object[] _getNFTInfo(byte[] tokenId)
        {

            byte[] v = Storage.Get(Storage.CurrentContext, tokenId);
            if (v.Length == 0)
                return new object[0];

            /*
            //老式实现方法
            NFTInfo info = new NFTInfo();
            int seek = 0;
            var ownerLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.owner = v.AsString().Substring(seek, ownerLen).AsByteArray();
            seek += ownerLen;

            int dataLen;

            dataLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            byte[] tmp = v.AsString().Substring(seek, dataLen).AsByteArray();
            seek += dataLen;

            info.isGestating = tmp[0] == 1;
            info.isReady = tmp[1] == 1;

            dataLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.cooldownIndex = v.AsString().Substring(seek, dataLen).AsByteArray().AsBigInteger();
            seek += dataLen;
            dataLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.nextActionAt = v.AsString().Substring(seek, dataLen).AsByteArray().AsBigInteger();
            seek += dataLen;

            dataLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.cloneWithId = v.AsString().Substring(seek, dataLen).AsByteArray().AsBigInteger();
            seek += dataLen;
            dataLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.birthTime = v.AsString().Substring(seek, dataLen).AsByteArray().AsBigInteger();
            seek += dataLen;

            dataLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.matronId = v.AsString().Substring(seek, dataLen).AsByteArray().AsBigInteger();
            seek += dataLen;

            dataLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.sireId = v.AsString().Substring(seek, dataLen).AsByteArray().AsBigInteger();
            seek += dataLen;

            dataLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.generation = v.AsString().Substring(seek, dataLen).AsByteArray().AsBigInteger();
            seek += dataLen;

            dataLen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.genesCode = v.AsString().Substring(seek, dataLen).AsByteArray();
            seek += dataLen;
            return (object[])(object)info;
            */

            //新式实现方法只要一行
            return (object[])Helper.Deserialize(v);
            // return Helper.Deserialize(v) as TransferInfo;
        }

        /**
         * 存储角斗士信息
         */
        private static void _putNFTInfo(byte[] tokenId, NFTInfo info)
        {
            /*
            // 用一个老式实现法
            byte[] nftInfo = _byteLen(info.owner.Length).Concat(info.owner);

            byte byteGest = info.isGestating == false ? (byte)0 : (byte)1;
            byte byteReady = info.isReady == false ? (byte)0 : (byte)1;
            byte[] tmp = new byte[] { byteGest, byteReady };

            nftInfo = nftInfo.Concat(_byteLen(tmp.Length)).Concat(tmp);
            nftInfo = nftInfo.Concat(_byteLen(info.cooldownIndex.AsByteArray().Length)).Concat(info.cooldownIndex.AsByteArray());
            nftInfo = nftInfo.Concat(_byteLen(info.nextActionAt.AsByteArray().Length)).Concat(info.nextActionAt.AsByteArray());
            nftInfo = nftInfo.Concat(_byteLen(info.cloneWithId.AsByteArray().Length)).Concat(info.cloneWithId.AsByteArray());
            nftInfo = nftInfo.Concat(_byteLen(info.birthTime.AsByteArray().Length)).Concat(info.birthTime.AsByteArray());
            nftInfo = nftInfo.Concat(_byteLen(info.matronId.AsByteArray().Length)).Concat(info.matronId.AsByteArray());
            nftInfo = nftInfo.Concat(_byteLen(info.sireId.AsByteArray().Length)).Concat(info.sireId.AsByteArray());
            nftInfo = nftInfo.Concat(_byteLen(info.generation.AsByteArray().Length)).Concat(info.generation.AsByteArray());

            nftInfo = nftInfo.Concat(_byteLen(info.genesCode.Length)).Concat(info.genesCode);
            */
            // 新式实现方法只要一行
            byte[] nftInfo = Helper.Serialize(info);

            Storage.Put(Storage.CurrentContext, tokenId, nftInfo);
        }

        /**
         * 获取交易信息
         */
        public static object[] getTXInfo(byte[] txid)
        {
            byte[] v = Storage.Get(Storage.CurrentContext, txid);
            if (v.Length == 0)
                return new object[0];

            /*
            //老式实现方法
            TransferInfo info = new TransferInfo();
            int seek = 0;
            var fromlen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.from = v.AsString().Substring(seek, fromlen).AsByteArray();
            seek += fromlen;
            var tolen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.to = v.AsString().Substring(seek, tolen).AsByteArray();
            seek += tolen;
            var valuelen = (int)v.AsString().Substring(seek, 2).AsByteArray().AsBigInteger();
            seek += 2;
            info.value = v.AsString().Substring(seek, valuelen).AsByteArray().AsBigInteger();
            return (object[])(object)info;
            */

            //新式实现方法只要一行
            return (object[])Helper.Deserialize(v);
            // return Helper.Deserialize(v) as TransferInfo;
        }

        /**
         * 存储交易信息
         */
        private static void _setTxInfo(byte[] from, byte[] to, BigInteger value)
        {
            //因为testnet 还在2.6，限制

            TransferInfo info = new TransferInfo();
            info.from = from;
            info.to = to;
            info.value = value;

            /*
            //用一个老式实现法
            byte[] txinfo = _byteLen(info.from.Length).Concat(info.from);
            txinfo = txinfo.Concat(_byteLen(info.to.Length)).Concat(info.to);
            byte[] _value = value.AsByteArray();
            txinfo = txinfo.Concat(_byteLen(_value.Length)).Concat(_value);
            */
            //新式实现方法只要一行
            byte[] txinfo = Helper.Serialize(info);

            byte[] txid = (ExecutionEngine.ScriptContainer as Transaction).Hash;

            Storage.Put(Storage.CurrentContext, txid, txinfo);
        }

        /**
         * 附加数据，暂不支持
         */
        public static Object tokenExtraData(BigInteger tokenId, string key)
        {
            return null;
        }

        /**
         * 某个地址拥有的角斗士数量，暂不支持获取
         */
        public static BigInteger balanceOf(byte[] owner)
        {
            return Storage.Get(Storage.CurrentContext, owner).AsBigInteger();
        }

        /**
         * 获取某个地址拥有的第index个角斗士信息，暂不支持获取
         */
        public static BigInteger tokenOfOwnerByIndex(byte[] owner, BigInteger index)
        {
            byte[] ownerKey = owner.Concat(index.AsByteArray());
            return Storage.Get(Storage.CurrentContext, ownerKey).AsBigInteger();
        }

        /**
         * 查看某个角斗士是否被授权，暂不支持
         */
        public static byte[] allowance(BigInteger tokenId)
        {
            byte[] approvalKey = "apr/".AsByteArray().Concat(tokenId.AsByteArray());
            return Storage.Get(Storage.CurrentContext, approvalKey);
        }

        /**
         * 获取某个地址拥有的所有角斗士id，暂不支持获取
         */
        public static BigInteger[] tokensOfOwner(byte[] owner)
        {
            BigInteger tokenCount = balanceOf(owner);
            BigInteger[] result = new BigInteger[(int)tokenCount];

            if (tokenCount == 0)
            {
                // Return an empty array
                return result;
            }
            else
            {
                // We count on the fact that all NFTInfo have IDs starting at 1 and increasing
                // sequentially up to the totalCat count.
                BigInteger idx;
                for (idx = 1; idx < tokenCount + 1; idx += 1)
                {
                    byte[] ownerKey = owner.Concat(idx.AsByteArray());
                    byte[] tokenId = Storage.Get(Storage.CurrentContext, ownerKey);

                    //result.Concat(_byteLen(tokenId.Length)).Concat(tokenId);
                    result[(int)idx - 1] = tokenId.AsBigInteger();
                }

                return result;
            }

        }

        //private static byte[] _byteLen(BigInteger n)
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

        //private static void _addOwnerToken(byte[] owner, BigInteger tokenId)
        //{
        //    byte[] balance = Storage.Get(Storage.CurrentContext, owner);
        //    BigInteger newBalance = balance.AsBigInteger() + 1;

        //    byte[] addKey = owner.Concat(newBalance.AsByteArray());
        //    Storage.Put(Storage.CurrentContext, addKey, tokenId);

        //    Storage.Put(Storage.CurrentContext, owner, newBalance);
        //}

        //private static bool _subOwnerToken(byte[] owner, BigInteger tokenId)
        //{
        //    StorageContext ctx = Storage.CurrentContext;

        //    byte[] balance = Storage.Get(ctx, owner);

        //    byte[] ownerKey;
        //    byte[] tId;
        //    byte[] lastTokenIdx;
        //    byte[] swapKey;
        //    byte[] swapToken;
        //    for (BigInteger idx = 1; idx < balance.AsBigInteger() + 1; idx += 1)
        //    {
        //        ownerKey = owner.Concat(idx.AsByteArray());
        //        tId = Storage.Get(ctx, ownerKey);
        //        if (tId.AsBigInteger() == tokenId)
        //        {
        //            lastTokenIdx = balance;
        //            swapKey = owner.Concat(lastTokenIdx);
        //            swapToken = Storage.Get(ctx, swapKey);
        //            Storage.Put(ctx, ownerKey, swapToken);
        //            Storage.Delete(ctx, swapKey);

        //            // log("removed token from owners list");

        //            BigInteger newBalance = balance.AsBigInteger() - 1;
        //            if (newBalance > 0)
        //            {
        //                Storage.Put(ctx, owner, newBalance);
        //            }
        //            else
        //            {
        //                Storage.Delete(ctx, owner);
        //            }

        //            return true;
        //        }
        //    }

        //    return false;
        //}

    }
}
