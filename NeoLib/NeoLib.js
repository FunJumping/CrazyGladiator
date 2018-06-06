
function NeoLib() {
    this.nftHash = "0xdd289cca76a4055d420bd6ab126b38542d1b6ee3";
    this.auctionHash = "0x097f911d7855a7bf9ee9d751fcf689d120e3c843";
    this.nep55Hash = "0xe52a08c20986332ad8dccf9ded38cc493878064a";
    
    NEOGAMESDK.init();

    this.urlParams = NEOGAMESDK.getUrlParams();
	this.address = this.urlParams.wallet;
	console.log('钱包地址');
	console.log(this.address);
}

NeoLib.prototype.getGenGladitorCount = function(callback) {
    var data = {
        'sbParamJson': [],
        'sbPushString': "totalSupply",
        'nnc': this.nftHash
    }
    NEOGAMESDK.invokescript(data, function (json) {
        console.log(json);
        var retVal = this.getOneBigInteger(json);
        callback(null, retVal);
    }.bind(this));
}

NeoLib.prototype.isReadyToBreed = function(glaId, callback) {
    var data = {
        'sbParamJson': ['(integer)' + glaId],
        'sbPushString': "isReadyToBreed",
        'nnc': this.nftHash
    }
    NEOGAMESDK.invokescript(data, function (json) {
        console.log(json);
        var retVal = this.getOneBigInteger(json);
        callback(null, retVal);
    }.bind(this));

}

NeoLib.prototype.isPregnant = function(glaId, callback) {
    var data = {
        'sbParamJson': ['(integer)' + glaId],
        'sbPushString': "isPregnant",
        'nnc': this.nftHash
    }
    NEOGAMESDK.invokescript(data, function (json) {
        console.log(json);
        var retVal = this.getOneBigInteger(json);
        callback(null, retVal);
    }.bind(this));
}

NeoLib.prototype.getGladitorByID = function(glaID, callback) {
    var data = {
        'sbParamJson': ['(integer)' + glaID],
        'sbPushString': "tokenData",
        'nnc': this.nftHash
    }
    NEOGAMESDK.invokescript(data, function (json) {
        var stack = json.info.stack;
        var item;
        var itemValue;
		console.log(json);
        var retVal = [];
        var genes = [];
        var tmp;
        if (stack.length > 0) {
            item = stack[0];
            if (item.type == "Array") {
                var aryItem = item.value;
                for(var i=0; i<aryItem.length; i++) {
                    itemValue = aryItem[i];
                    if(itemValue.type == "Integer") {
                        var bytes = itemValue.value;
                        if ((bytes.length & 1) == 0) {
                            bytes = itemValue.value.hexToBytes();
                        }
                        itemValue.value = new Neo.BigInteger(bytes).toUint8Array().toHexString();
                    }
                    else if(itemValue.type == "Boolean") {
                        if(!itemValue.value || itemValue.value=="false") {
                            itemValue.value = 0;
                        }
                        else {
                            itemValue.value = 1;
                        }
                    }
                }

                var bs = item.value[0].value.hexToBytes();
                tmp = ThinNeo.Helper.GetAddressFromScriptHash(bs);
				retVal.push(tmp); // owner
				
				/*
					1.isGestating, 2.isReady, 3.cooldownIndex, 4.nextActionAt, 5.cloneWithId,
					6.birthTime, 7.matronId, 8.sireId, 9.generation
				*/
				for (var j = 1; j < 10; ++j) {
					if (item.value[j].type == "Integer") {
						tmp = parseInt(item.value[j].value);
					} else if (item.value[j].type == "ByteArray") {
						bs = item.value[j].value.hexToBytes();
						tmp = new Neo.BigInteger(bs).toInt32();
					} else {
						tmp = 0;
					}
                    if(isNaN(tmp))
                        tmp = 0;
					retVal.push(tmp);
				}

                // int strength, power, ...
                for(var j=10; j<item.value.length; ++j) {
					if (item.value[j].type == "Integer") {
						tmp = parseInt(item.value[j].value);
					} else if (item.value[j].type == "ByteArray") {
						bs = item.value[j].value.hexToBytes();
						tmp = new Neo.BigInteger(bs).toInt32();
					} else {
						tmp = 0;
					}
                    if(isNaN(tmp))
                        tmp = 0;
                    genes.push(tmp);
                }
            }
        }

        var attGene = {};
        if(genes.length>5) {
            attGene.strength = genes[0];
            attGene.power  = genes[1];
            attGene.agile  = genes[2];
            attGene.speed  = genes[3];
            attGene.skill1 = genes[4];

            attGene.skill2 = genes[5];
            attGene.skill3 = genes[6];
            attGene.skill4 = genes[7];
            attGene.skill5 = genes[8];
            attGene.equip1 = genes[9];

            attGene.equip2 = genes[10];
            attGene.equip3 = genes[11];
            attGene.equip4 = genes[12];
            attGene.restrictAttribute = genes[13];
            attGene.character = genes[14];

			attGene.part1 = genes[15];
            attGene.part2 = genes[16];
            attGene.part3 = genes[17];
            attGene.part4 = genes[18];
            attGene.part5 = genes[19];
            attGene.cPart1 = genes[20];
            attGene.cPart2 = genes[21];
        }
        retVal.push(attGene);

        callback(null, retVal);
    }.bind(this));
}

NeoLib.prototype.ownerOfByID = function(glaID, callback) {
    var data = {
        'sbParamJson': ['(integer)' + glaID],
        'sbPushString': "ownerOf",
        'nnc': this.nftHash
    }
    NEOGAMESDK.invokescript(data, function (json) {
        console.log(json);
        var stack = json.info.stack;
        var item;
        var retVal = null;
        if (stack.length > 0) {
            item = stack[0];
            if (item.type == "String")
                retVal = item.value;
            else if (item.type == "ByteArray") {
                var bs = item.value.hexToBytes();
                retVal = ThinNeo.Helper.GetAddressFromScriptHash(bs);
            }
        }
        callback(null, retVal);
    }.bind(this));

}

NeoLib.prototype.getAuctionAddr = function(callback) {
    var data = {
        'sbParamJson': [],
        'sbPushString': "getAuctionAddr",
        'nnc': this.nftHash
    }
    NEOGAMESDK.invokescript(data, function (json) {
        console.log(json);
        var stack = json.info.stack;
        var item;
        var retVal = null;
		console.log(json);
        if (stack.length > 0) {
            item = stack[0];
            if (item.type == "String")
                retVal = item.value;
            else if (item.type == "ByteArray") {
                if(item.value) {
                    var bs = item.value.hexToBytes();
                    retVal = ThinNeo.Helper.GetAddressFromScriptHash(bs);
                }
            }
            else {
                retVal = item.value;
            }
        }
        callback(null, retVal);
    }.bind(this));

}

NeoLib.prototype.allowance = function(tokenId, callback) {
    var data = {
        'sbParamJson': ['(integer)' + tokenId],
        'sbPushString': "allowance",
        'nnc': this.nftHash
    }
    NEOGAMESDK.invokescript(data, function (json) {
        console.log(json);
        var stack = json.info.stack;
        var item;
        var retVal = null;
        if (stack.length > 0) {
            item = stack[0];
            if (item.type == "String")
                retVal = item.value;
            else if (item.type == "ByteArray") {
                if (item.value != "") {
                    var bs = item.value.hexToBytes();
                    retVal = ThinNeo.Helper.GetAddressFromScriptHash(bs);
                }
            }
            else {
                retVal = item.value;
            }
        }
        callback(null, retVal);
    }.bind(this));
}

NeoLib.prototype.tokenURI = function(tokenId, callback) {
    var data = {
        'sbParamJson': [],
        'sbPushString': "tokenURI",
        'nnc': this.nftHash
    }
    NEOGAMESDK.invokescript(data, function (json) {
        console.log(json);
        var stack = json.info.stack;
        var item;
        var retVal;
        item = stack[0];
        retVal = null;
        if (item.type == "String")
            retVal = item.value;
        else if (item.type == "ByteArray") {
            console.log(item.value);
            var bs = item.value.hexToBytes();
            retVal = ThinNeo.Helper.Bytes2String(bs);
        }
        else {
            retVal = item.value;
        }
        if (!retVal) {
            retVal = 0;
        }
        callback(null, retVal);
    }.bind(this));
}

NeoLib.prototype.transfer = function(from, to, tokenId, callback) {
    var data = {
        'sbParamJson': ["(address)" + from, "(address)" + to, "(integer)" + tokenId],
        'sbPushString': "transfer",
        'nnc': this.nftHash
    }
    NEOGAMESDK.makeRawTransaction(data, callback);
}

NeoLib.prototype.transferFrom = function(from, to, tokenId, callback) {
    var data = {
        'sbParamJson': ["(address)" + from, "(address)" + to, "(integer)" + tokenId],
        'sbPushString': "transferFrom",
        'nnc': this.nftHash
    }
    NEOGAMESDK.makeRawTransaction(data, callback);
}

NeoLib.prototype.setAuctionAddr = function(auctionAddr, callback) {
    var data = {
        'sbParamJson': ["(address)" + auctionAddr],
        'sbPushString': "setAuctionAddr",
        'nnc': this.nftHash
    }
    NEOGAMESDK.makeRawTransaction(data, callback);
}

NeoLib.prototype.setSgas = function(sgasHash, callback) {
    var data = {
        'sbParamJson': ["(hex)" + sgasHash],
        'sbPushString': "_setSgas",
        'nnc': this.nftHash
    }
    NEOGAMESDK.makeRawTransaction(data, callback);
}

NeoLib.prototype.giveBirth = function(glaId, callback) {
    var data = {
        'sbParamJson': ["(integer)" + glaId],
        'sbPushString': "giveBirth",
        'nnc': this.nftHash
    }
    NEOGAMESDK.makeRawTransaction(data, callback);
}

NeoLib.prototype.setAttrConfig = function(normalSkillIdMax, rareSkillIdMax, normalEquipIdMax, rareEquipIdMax,
     atr0Max, atr1Max, atr2Max, atr3Max, atr4Max, atr5Max, atr6Max, atr7Max, atr8Max, callback) {
         
    var data = {
        'sbParamJson': ["(integer)" + normalSkillIdMax, "(integer)" + rareSkillIdMax, 
        "(integer)" + normalEquipIdMax, "(integer)" + rareEquipIdMax,
        "(integer)" + atr0Max, "(integer)" + atr1Max, "(integer)" + atr2Max, "(integer)" + atr3Max, "(integer)" + atr4Max,
        "(integer)" + atr5Max, "(integer)" + atr6Max, "(integer)" + atr7Max, "(integer)" + atr8Max
    ],
        'sbPushString': "setAttrConfig",
        'nnc': this.nftHash
    }
    NEOGAMESDK.makeRawTransaction(data, callback);
}

// 克隆拍卖创建
NeoLib.prototype.createSaleAuction = function(tokenId, startPrice, endPrice, duration, callback) {
    var data = {
        'sbParamJson': ["(address)" + this.address, "(integer)" + tokenId, 
        "(integer)" + Neo.Fixed8.fromNumber(startPrice).getData(), "(integer)" + Neo.Fixed8.fromNumber(endPrice).getData(), "(integer)" + duration],
        'sbPushString': "createSaleAuction",
        'nnc': this.auctionHash
    }
    NEOGAMESDK.makeRawTransaction(data, function(res){
        console.log(res);
        if (res.err == false) {
            callback(null, res.info.txid);

            var txData = new TxData();
            txData.txid = res.info.txid;
            txData.txType = "createSaleAuction";
            txData.tokenId = tokenId;
            txData.data = startPrice;
            TxinfoProxy.instance.addTxinfo(txData);
        }
        else {
            console.log('Error 交易有错误:'+res.info);
        }
    }.bind(this));
}

NeoLib.prototype.createCloneAuction = function(tokenId, startPrice, endPrice, duration, callback) {
    var data = {
        'sbParamJson': ["(address)" + this.address, "(integer)" + tokenId, 
        "(integer)" + Neo.Fixed8.fromNumber(startPrice).getData(), "(integer)" + Neo.Fixed8.fromNumber(endPrice).getData(), "(integer)" + duration],
        'sbPushString': "createCloneAuction",
        'nnc': this.auctionHash
    }
    NEOGAMESDK.makeRawTransaction(data, function(res){
        console.log(res);
        if (res.err == false) {
            callback(null, res.info.txid);

            var txData = new TxData();
            txData.txid = res.info.txid;
            txData.txType = "createCloneAuction";
            txData.tokenId = tokenId;
            txData.data = startPrice;
            TxinfoProxy.instance.addTxinfo(txData);
        }
        else {
            console.log('Error 交易有错误:'+res.info);
        }
    }.bind(this));
}

NeoLib.prototype.cancelAuction = function(owner, glaId, callback) {
    var data = {
        'sbParamJson': ["(address)" + owner, "(integer)" + glaId],
        'sbPushString': "cancelAuction",
        'nnc': this.auctionHash
    }
    NEOGAMESDK.makeRawTransaction(data, function(res){
        console.log(res);
        if (res.err == false) {
            callback(null, res.info.txid);

            var txData = new TxData();
            txData.txid = res.info.txid;
            txData.txType = "cancelAuction";
            txData.tokenId = glaId;
            TxinfoProxy.instance.addTxinfo(txData);
        }
        else {
            console.log('Error 交易有错误:'+res.info);
        }
    }.bind(this));
}

NeoLib.prototype.breedWithMy = function(owner, motherId, fatherId, callback) {
    var data = {
        'sbParamJson': ["(address)" + owner, "(integer)" + motherId, "(integer)" + fatherId],
        'sbPushString': "breedWithMy",
        'nnc': this.auctionHash
    }
    NEOGAMESDK.makeRawTransaction(data, function(res){
        console.log(res);
        if (res.err == false) {
            callback(null, res.info.txid);

            var txData = new TxData();
            txData.txid = res.info.txid;
            txData.txType = "breedWithMy";
            txData.tokenId = motherId;
            txData.tokenId2 = fatherId;
            TxinfoProxy.instance.addTxinfo(txData);
        }
        else {
            console.log('Error 交易有错误:'+res.info);
        }
    }.bind(this));
}

NeoLib.prototype.drawToContractOwner = function(count, callback) {
    var data = {
        'sbParamJson': ["(integer)" + count],
        'sbPushString': "drawToContractOwner",
        'nnc': this.auctionHash
    }
    NEOGAMESDK.makeRawTransaction(data, callback);
}

NeoLib.prototype.buyOnAuction = function(tokenId, callback) {
    var data = {
        'sbParamJson': ["(address)" + this.address, "(integer)" + tokenId],
        'sbPushString': "buyOnAuction",
        'nnc': this.auctionHash
    };
    
    NEOGAMESDK.makeRawTransaction(data, function(res){
        console.log(res);
        if (res.err == false) {
            callback(null, res.info.txid);

            var txData = new TxData();
            txData.txid = res.info.txid;
            txData.txType = "buyOnAuction";
            txData.tokenId = tokenId;
            TxinfoProxy.instance.addTxinfo(txData);
        }
        else {
            console.log('Error 交易有错误:'+res.info);
        }
    }.bind(this));
}

NeoLib.prototype.cloneOnAuction = function(bidMyGlaId, bidCloneId, callback) {
    var data = {
        'sbParamJson': ["(address)" + this.address, "(integer)" + bidMyGlaId, "(integer)" + bidCloneId],
        'sbPushString': "cloneOnAuction",
        'nnc': this.auctionHash
    }
    NEOGAMESDK.makeRawTransaction(data, function(res){
        console.log(res);
        if (res.err == false) {
            callback(null, res.info.txid);

            var txData = new TxData();
            txData.txid = res.info.txid;
            txData.txType = "cloneOnAuction";
            txData.tokenId = bidMyGlaId;
            txData.tokenId2 = bidCloneId;
            TxinfoProxy.instance.addTxinfo(txData);
        }
        else {
            console.log('Error 交易有错误:'+res.info);
        }
    }.bind(this));
}

// 获取拍卖信息
NeoLib.prototype.getAuctionById = function(glaID, callback) {
    var data = {
        'sbParamJson': ['(integer)' + glaID],
        'sbPushString': "getAuctionById",
        'nnc': this.nftHash
    }
    NEOGAMESDK.invokescript(data, function (json) {
        console.log(json);
        var stack = json.info.stack;
        var item;
        var retVal = [];
        var tmp;
        var bytes;
        if (stack.length > 0) {
            item = stack[0];
            if (item.type == "Array") {
                var bs = item.value[0].value.hexToBytes();
                tmp = ThinNeo.Helper.GetAddressFromScriptHash(bs);
                retVal.push(tmp);

                tmp = item.value[1].value;
                if(!tmp) {
                    tmp = 0;
                }
                retVal.push(tmp);
                
                bytes = (item.value[2].value+"").hexToBytes();
                tmp = new Neo.BigInteger(bytes);
                retVal.push(tmp);
                
                bytes = (item.value[3].value+"").hexToBytes();
                tmp = new Neo.BigInteger(bytes);
                retVal.push(tmp);
                
                bytes = (item.value[4].value+"").hexToBytes();
                tmp = new Neo.BigInteger(bytes);
                retVal.push(tmp);

                bytes = (item.value[5].value+"").hexToBytes();
                tmp = new Neo.BigInteger(bytes);
                retVal.push(tmp + "");
            }
        }
        callback(null, retVal);
    }.bind(this));
}

NeoLib.prototype.mintTokenNep = async function(count, callback) {
    var data = {"count": count};

    NEOGAMESDK.makeMintTokenTransaction(data, function(res){
        console.log(res);
        if (res.err == false) {
            alert(res.info.txid);
            callback(null, [!res.err, res.info.txid])
        }
        else {
            alert('交易有错误:'+res.info);
        }
    })
}

NeoLib.prototype.transferNep = async function(from, to, count, callback) {
    var data = {
        'sbParamJson': ["(address)" + from, "(address)" + to, "(integer)" + Neo.Fixed8.fromNumber(count).getData()],
        'sbPushString': "transfer",
        'nnc': this.nep55Hash
    }
    NEOGAMESDK.makeRawTransaction(data, callback);
}

// 充值到拍卖行
NeoLib.prototype.transferNepToAuc = async function(from, to, count, callback) {
    this.transferNep(from, to, count, function(res) {
        if(!res.err) {
            var txid = res.info.txid;
            this.rechargeToken(from, txid, function(res){
                console.log(res);
                if (res.err == false) {
                    callback(null, res.info.txid);
        
                    var txData = new TxData();
                    txData.txid = res.info.txid;
                    txData.txType = "transferNepToAuc";
                    txData.data = count;
                    TxinfoProxy.instance.addTxinfo(txData);
                }
                else {
                    console.log('Error 交易有错误:'+res.info);
                }
            }.bind(this));
        }
    }.bind(this));
}

// 充值
NeoLib.prototype.rechargeToken = async function(owner, recTxid, callback) {
    var data = {
        'sbParamJson': ["(address)" + owner, "(hexinteger)" + recTxid ],
        'sbPushString': "rechargeToken",
        'nnc': this.auctionHash
    }
    NEOGAMESDK.makeRawTransaction(data, callback);
}

// 
NeoLib.prototype.drawTokenAuc = async function(owner, count, callback) {
    var data = {
        'sbParamJson': ["(address)" + owner, "(integer)" + Neo.Fixed8.fromNumber(count).getData() ],
        'sbPushString': "drawToken",
        'nnc': this.auctionHash
    }
    NEOGAMESDK.makeRawTransaction(data, callback);
}

NeoLib.prototype.refund = async function(count, callback) {
    var data = {
        'count': count
    }
    NEOGAMESDK.makeRefundTransaction(data, callback);
}

NeoLib.prototype.balanceOfNep = function(addr, callback) {
    var data = {
        'sbParamJson': ['(addr)' + addr],
        'sbPushString': "balanceOf",
        'nnc': this.nep55Hash
    }
    NEOGAMESDK.invokescript(data, function (json) {
        console.log(json);
        var stack = json.info.stack;
        var item;
        var retVal = null;
        var retVal2 = null;
        if (stack.length > 0) {
            item = stack[0];
            console.log(item);
            var bytes = item.value.hexToBytes();
            retVal = new Neo.BigInteger(bytes);
        }
        callback(null, retVal);
    }.bind(this));
}

NeoLib.prototype.totalSupplyNep = function(callback) {
    var data = {
        'sbParamJson': [],
        'sbPushString': "totalSupply",
        'nnc': this.nep55Hash
    }
    NEOGAMESDK.invokescript(data, function (json) {
        console.log(json);
        var stack = json.info.stack;
        var item;
        var retVal = null;
        var retVal2 = null;
        if (stack.length > 0) {
            item = stack[0];
            console.log(item);
            var bytes = item.value.hexToBytes();
            retVal = new Neo.BigInteger(bytes);
        }
        callback(null, retVal);
    }.bind(this));
}

NeoLib.prototype.totalSupplyAuct = function(callback) {
    var data = {
        'sbParamJson': [],
        'sbPushString': "totalSupply",
        'nnc': this.auctionHash
    }
    NEOGAMESDK.invokescript(data, function (json) {
        console.log(json);
        var stack = json.info.stack;
        var item;
        var retVal = null;
        var retVal2 = null;
        if (stack.length > 0) {
            item = stack[0];
            console.log(item);
            var bytes = item.value.hexToBytes();
            retVal = new Neo.BigInteger(bytes);
        }
        callback(null, retVal);
    }.bind(this));
}

NeoLib.prototype.balanceOfAuc = function(addr, callback) {
    var data = {
        'sbParamJson': ['(addr)' + addr],
        'sbPushString': "balanceOf",
        'nnc': this.auctionHash
    }
    NEOGAMESDK.invokescript(data, function (json) {
        console.log(json);
        var stack = json.info.stack;
        var item;
        var retVal = null;
        var retVal2 = null;
        if (stack.length > 0) {
            item = stack[0];
            console.log(item);
            var bytes = item.value.hexToBytes();
            retVal = new Neo.BigInteger(bytes);
        }
        callback(null, retVal);
    }.bind(this));
}

NeoLib.prototype.approve = function(approved, tokenId, callback) {
    var data = {
        'sbParamJson': ["(address)" + approved, "(integer)" + tokenId],
        'sbPushString': "approve",
        'nnc': this.nftHash
    }
    NEOGAMESDK.makeRawTransaction(data, callback);
}

NeoLib.prototype.getOneBigInteger = function(json) {
    var stack = json.info.stack;
    var item;
    var retVal = 0;
    if (stack.length > 0) {
        item = stack[0];
        if (item.type == "String") {
            retVal = item.value;
        }
        else if(item.type == "ByteArray") {
            var bytes = item.value.hexToBytes();
            retVal = new Neo.BigInteger(bytes);
        } else {
            retVal = item.value;
        }
    }
    return retVal;
}

NeoLib.prototype.mintToken = function(owner, strength, power, agile, speed,
    skill1, skill2, skill3, skill4, skill5, equip1, equip2, equip3, equip4,
    restrictAttribute, character, cPart1, cPart2, cPart3, cPart4, cPart5,
    cPart6, part1, part2, part3, part4, part5, part6, part7,
    part8, part9, part10, part11, part12,
     callback) {

    var data = {
        'sbParamJson': ["(address)" + owner,
			"(integer)" + strength,
			"(integer)" + power,
			"(integer)" + agile,
            "(integer)" + speed,
            
			"(integer)" + skill1,
			"(integer)" + skill2,
			"(integer)" + skill3,
			"(integer)" + skill4,
            "(integer)" + skill5,
                          
			"(integer)" + equip1,
			"(integer)" + equip2,
			"(integer)" + equip3,
            "(integer)" + equip4,
            
            "(integer)" + restrictAttribute,
            "(integer)" + character,
			
            "(integer)" + part1,
            "(integer)" + part2,
            "(integer)" + part3,
            "(integer)" + part4,
            "(integer)" + part5,
            "(integer)" + cPart1,
            "(integer)" + cPart2
			],
        'sbPushString': "mintToken",
        'nnc': this.nftHash
    }
    NEOGAMESDK.makeRawTransaction(data, callback);
}

NeoLib.prototype.createGen0Auction = function(strength, power, agile, speed,
    skill1, skill2, skill3, skill4, skill5, equip1, equip2, equip3, equip4,
    restrictAttribute, cPart1, cPart2, cPart3, cPart4, cPart5,
    cPart6, part1, part2, part3, part4, part5, part6, part7,
    part8, part9, part10, part11, part12, character,
     callback) {
    var data = {
        'sbParamJson': [
			"(integer)" + strength,
			"(integer)" + power,
			"(integer)" + agile,
            "(integer)" + speed,
            
			"(integer)" + skill1,
			"(integer)" + skill2,
			"(integer)" + skill3,
			"(integer)" + skill4,
            "(integer)" + skill5,
            
			"(integer)" + equip1,
			"(integer)" + equip2,
			"(integer)" + equip3,
            "(integer)" + equip4,
            
            "(integer)" + restrictAttribute,
            "(integer)" + character,

            "(integer)" + part1,
            "(integer)" + part2,
            "(integer)" + part3,
            "(integer)" + part4,
            "(integer)" + part5,
            "(integer)" + cPart1,
            "(integer)" + cPart2
			],
        'sbPushString': "createGen0Auction",
        'nnc': this.auctionHash
    }
    NEOGAMESDK.makeRawTransaction(data, callback);
}
