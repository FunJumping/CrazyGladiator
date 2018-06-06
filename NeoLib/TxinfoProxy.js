
function TxData() {
    this.txid = null;
    this.txType = "";
    this.tokenId = null;
    this.tokenId2 = null;
    this.isOK = false;
    this.data = null;
}

var TxinfoProxy = (function () {
    function TxinfoProxy() {
        this.sureTxInfos = {};
        this.unSureTxInfos = [];
		this._caller = null;
        this._callback = null;
    }
    Object.defineProperty(TxinfoProxy, "instance", {
        get: function () {
            if (!this._instance) {
                this._instance = new TxinfoProxy();
            }
            return this._instance;
        },
        enumerable: true,
        configurable: true
    });
    TxinfoProxy.prototype.setTxinfoLoadCallback = function (caller, callback) {
		this._caller = caller;
		this._callback = callback;
    };
    TxinfoProxy.prototype.addTxinfo = function (txData) {
        this.unSureTxInfos.push(txData);
        this.checkTick();
    };
    TxinfoProxy.prototype.checkTick = function () {
        (async function doStuff() {
            if (this.unSureTxInfos.length > 0) {
                var first = this.unSureTxInfos[0];
                if (!first.isLoading) {
                    first.isLoading = true;
                    var json = await this.getTXInfo(first.txid);
                    //console.log(json);
                    first.isLoading = false;

                    if(!json.hasOwnProperty("error") &&
                     json["result"].hasOwnProperty("confirmations") && json["result"]["confirmations"]>0 ) {
                        // tx ok
                        console.log("confirm ok");
                        this.unSureTxInfos.shift();

						if(this._callback != null) {
							this._callback.call(this._caller, first);
						}
                    }
        
                    setTimeout(doStuff.bind(this), 4000);
                }
            }
        }.bind(this)());
    };
    TxinfoProxy.prototype.getTXInfo = function (txId) {
        var nodeUrl = "http://115.159.105.217:20332/";
        var str = this.makeRpcUrl(nodeUrl, "getrawtransaction", txId, 1);
        //console.log(str);
        var result = fetch(str, { "method": "get" }).then(function (resp) {
            return resp.json();
        });
        return result;
    };
    TxinfoProxy.prototype.makeRpcUrl = function (url, method) {
        var args = [];
        for (var _a = 2; _a < arguments.length; _a++) {
            args[_a - 2] = arguments[_a];
        }
        var _params = [];
        for (var _i = 2; _i < arguments.length; _i++) {
            _params[_i - 2] = arguments[_i];
        }
        if (url[url.length - 1] != '/')
            url = url + "/";
        var urlout = url + "?jsonrpc=2.0&id=1&method=" + method + "&params=[";
        for (var i = 0; i < _params.length; i++) {
            urlout += JSON.stringify(_params[i]);
            if (i != _params.length - 1)
                urlout += ",";
        }
        urlout += "]";
        return urlout;
    };
    return TxinfoProxy;
}());
