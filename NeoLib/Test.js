
var NEOGAMESDK = function () {
    this.utxoUtil = new UtxoUtil();
    this.nodeUrl = "http://10.1.6.48:10332/";
    this.nftHash = "0xccab4cee886dd58f17b32eff16d5e59961113a4c";
    this.auctionHash = "0xa879326b275885da20dd29d8c93979ae233fe772";
    this.nep55Hash = "0xd1008e554323d99ecda8c296c2a2afbe36897dd2";
	
    this.wallet = new Wallet();
    this.wallet.setPriKeyByWif("L3RxVmjFsike9kbAmGYcwZDWUr1SWmNhHgeAZx7nioSbyGbjWNtP");
	
	return this;
}();

NEOGAMESDK.init = function () {
	console.log('NEOGAMESDK.init()');

};

NEOGAMESDK.getUrlParams = function () {
	var obj = {};
	obj.wallet = "AcKA1A3TRx6ubNzi3Dz2QFW6V9uEkeVasg";
	return obj;
};

NEOGAMESDK.makeMintTokenTransaction = async function (dataP, callback) {
	var count = dataP.count;
	
	var nnc = this.nep55Hash;
	
	var utxos = await this.utxoUtil.getutxo(this.wallet.address);
	var pkeyhash = ThinNeo.Helper.GetPublicKeyScriptHashFromPublicKey(this.wallet.pubKey);
	let targeraddr = this.wallet.address;
	
	var array = nnc.hexToBytes().reverse();
	var nepAddress = ThinNeo.Helper.GetAddressFromScriptHash(array);
	
	let tran = this.makeTran(this.utxoUtil.getassets(utxos), nepAddress, this.wallet.idGAS, Neo.Fixed8.fromNumber(count));
	tran.type = ThinNeo.TransactionType.InvocationTransaction;
	tran.extdata = new ThinNeo.InvokeTransData();
	let script = null;
	var sb = new ThinNeo.ScriptBuilder();
	var scriptaddress = nnc.hexToBytes().reverse();
	sb.EmitParamJson([]);
	sb.EmitPushString("mintTokens");
	sb.EmitAppCall(scriptaddress);
	tran.extdata.script = sb.ToArray();
	tran.extdata.gas = Neo.Fixed8.fromNumber(1.0);
	var msg = tran.GetMessage();
	var signdata = ThinNeo.Helper.Sign(msg, this.wallet.priKey);
	tran.AddWitness(signdata, this.wallet.pubKey, this.wallet.address);
	let txid = tran.GetHash().clone().reverse().toHexString();
	var data = tran.GetRawData();
	var scripthash = data.toHexString();
	var json = await this.wallet.sendRaw(this.nodeUrl, scripthash);
	var r = await json["result"];
	//return [r, txid];
	var res = {"err":!r, "info":{"txid":txid}};
	callback(res);
};

NEOGAMESDK.makeTran = function(utxos, targetaddr, assetid, sendcount) {
    var tran = new ThinNeo.Transaction();
    tran.type = ThinNeo.TransactionType.ContractTransaction;
    tran.version = 0;
    tran.extdata = null;
    tran.attributes = [];
    tran.inputs = [];
    var scraddr = "";
    if (utxos[assetid]) {
        utxos[assetid].sort((a, b) => {
            return a.count.compareTo(b.count);
        });
    }
    var us = utxos[assetid];
    var count = Neo.Fixed8.Zero;
    if (us) {
        for (var i = 0; i < us.length; i++) {
            var input = new ThinNeo.TransactionInput();
            input.hash = us[i].txid.hexToBytes().reverse();
            input.index = us[i].n;
            input["_addr"] = us[i].addr;
            tran.inputs.push(input);
            count = count.add(us[i].count);
            scraddr = us[i].addr;
            if (count.compareTo(sendcount) > 0) {
                break;
            }
        }
    }
    if (count.compareTo(sendcount) >= 0) {
        tran.outputs = [];
        if (sendcount.compareTo(Neo.Fixed8.Zero) > 0) {
            var output = new ThinNeo.TransactionOutput();
            output.assetId = assetid.hexToBytes().reverse();
            output.value = sendcount;
            output.toAddress = ThinNeo.Helper.GetPublicKeyScriptHash_FromAddress(targetaddr);
            tran.outputs.push(output);
        }
        var change = count.subtract(sendcount);
        if (change.compareTo(Neo.Fixed8.Zero) > 0) {
            var outputchange = new ThinNeo.TransactionOutput();
            outputchange.toAddress = ThinNeo.Helper.GetPublicKeyScriptHash_FromAddress(scraddr);
            outputchange.value = change;
            outputchange.assetId = assetid.hexToBytes().reverse();
            tran.outputs.push(outputchange);
        }
    }
    else {
        throw new Error("No enough money.");
    }
    return tran;
}


function UtxoUtil() {

}

UtxoUtil.prototype.getutxo = function(addr) {
    return __awaiter(this, void 0, void 0, function* () {
        var api = "http://10.1.6.48:20666";
        var str = WWW.makeRpcUrl(api, "getutxo", addr);
        console.log(str);
        var result = yield fetch(str, { "method": "get" });
        var json = yield result.json();
        var r = json["result"];
        return r;
    });
}

UtxoUtil.prototype.getassets = function(utxos) {
    var assets = {};
    for (var i in utxos) {
        var item = utxos[i];
        var txid = item.txid;
        var n = item.n;
        var asset = item.asset;
        var count = item.value;
        if (assets[asset] == undefined) {
            assets[asset] = [];
        }
        var utxo = {
            addr: item.addr,
            asset: asset,
            n: n,
            txid: txid,
            count: Neo.Fixed8.parse(count + "")
        };
        assets[asset].push(utxo);
    }
    return assets;
}
