
var NEOGAMESDK = /** @class */ (function () {
    function NEOGAMESDK() {
    }
    NEOGAMESDK.postMessagge = function (event) {
        if (event.data) {
            var data = JSON.parse(event.data);
            switch (data.cmd) {
                case "getUserInfoRes":
                    // 获取登录用户信息
                    console.log('NEOGAMESDK.getUserInfoRes.callback_getUserInfo');
                    console.log(data.data);
                    NEOGAMESDK.callback_getUserInfo[data.eventId](data.data);
                    delete NEOGAMESDK.callback_getUserInfo[data.eventId];
                    break;
                case "getSgasBalanceRes":
                    // 获取sgas余额
                    console.log('NEOGAMESDK.getSgasBalanceRes.callback_getSgasBalance');
                    console.log(data.data);
                    NEOGAMESDK.callback_getSgasBalance[data.eventId](data.data);
                    delete NEOGAMESDK.callback_getSgasBalance[data.eventId];
                    break;
                case "makeRefundTransactionRes":
                    console.log('NEOGAMESDK.makeRefundTransactionRes.callback_makeRefundTransaction');
                    console.log(data.data);
                    NEOGAMESDK.callback_makeRefundTransaction[data.eventId](data.data);
                    delete NEOGAMESDK.callback_makeRefundTransaction[data.eventId];
                    break;
                case "makeMintTokenTransactionRes":
                    // gas => sgas
                    console.log('NEOGAMESDK.makeMintTokenTransactionRes.callback_makeMintTokenTransaction');
                    console.log(data.data);
                    NEOGAMESDK.callback_makeMintTokenTransaction[data.eventId](data.data);
                    delete NEOGAMESDK.callback_makeMintTokenTransaction[data.eventId];
                    break;
                case "makeRawTransactionRes":
                    // 合约交易
                    console.log('NEOGAMESDK.makeRawTransactionRes.callback_makeRawTransaction');
                    console.log(data.data);
                    NEOGAMESDK.callback_makeRawTransaction[data.eventId](data.data);
                    delete NEOGAMESDK.callback_makeRawTransaction[data.eventId];
                    break;
                case "invokescriptRes":
                    // 合约读取
                    console.log('NEOGAMESDK.invokescriptRes.callback_invokescript');
                    console.log(data.data);
                    NEOGAMESDK.callback_invokescript[data.eventId](data.data);
                    delete NEOGAMESDK.callback_invokescript[data.eventId];
                    break;
            }
        }
    };
    NEOGAMESDK.getRandom = function (callback) {
        var finished = false;
        do {
            var id = Math.random();
            if (!callback || !callback.hasOwnProperty(id)) {
                finished = true;
            }
        } while (finished === false);
        return id;
    };
    NEOGAMESDK.init = function () {
        console.log('NEOGAMESDK.init()');
        if (NEOGAMESDK.is_init === false) {
            window.addEventListener("message", NEOGAMESDK.postMessagge);
        }
        NEOGAMESDK.is_init = true;
    };
    NEOGAMESDK.getUserInfo = function (callback) {
        console.log('NEOGAMESDK.getUserInfo( callback )');
        if (NEOGAMESDK.is_init === false) {
            console.log("please use init first !");
            return;
        }
        var eventId = NEOGAMESDK.getRandom(NEOGAMESDK.callback_getUserInfo);
        NEOGAMESDK.callback_getUserInfo[eventId] = callback;
        console.log('NEOGAMESDK.getUserInfo.eventId', eventId);
        var cmd = { "cmd": "getUserInfo", "eventId": eventId };
        parent.window.postMessage(JSON.stringify(cmd), "*");
    };
    NEOGAMESDK.getSgasBalance = function (callback) {
        console.log('NEOGAMESDK.getSgasBalance( callback )');
        if (NEOGAMESDK.is_init === false) {
            console.log("please use init first !");
            return;
        }
        var eventId = NEOGAMESDK.getRandom(NEOGAMESDK.callback_getSgasBalance);
        NEOGAMESDK.callback_getSgasBalance[eventId] = callback;
        console.log('NEOGAMESDK.getSgasBalance.eventId', eventId);
        var cmd = { "cmd": "getSgasBalance", "eventId": eventId };
        parent.window.postMessage(JSON.stringify(cmd), "*");
    };
    NEOGAMESDK.makeRefundTransaction = function (data, callback) {
        console.log('NEOGAMESDK.makeRefundTransaction( data, callback) ');
        if (NEOGAMESDK.is_init === false) {
            console.log("please use init first !");
            return;
        }
        var eventId = NEOGAMESDK.getRandom(NEOGAMESDK.callback_makeRefundTransaction);
        NEOGAMESDK.callback_makeRefundTransaction[eventId] = callback;
        console.log('NEOGAMESDK.makeRefundTransaction.eventId', eventId);
        var cmd = { "cmd": "makeRefundTransaction", "eventId": eventId, "data": data };
        parent.window.postMessage(JSON.stringify(cmd), "*");
    };
    NEOGAMESDK.makeMintTokenTransaction = function (data, callback) {
        console.log('NEOGAMESDK.makeMintTokenTransaction( data, callback) ');
        if (NEOGAMESDK.is_init === false) {
            console.log("please use init first !");
            return;
        }
        var eventId = NEOGAMESDK.getRandom(NEOGAMESDK.callback_makeMintTokenTransaction);
        NEOGAMESDK.callback_makeMintTokenTransaction[eventId] = callback;
        console.log('NEOGAMESDK.makeMintTokenTransaction.eventId', eventId);
        var cmd = { "cmd": "makeMintTokenTransaction", "eventId": eventId, "data": data };
        parent.window.postMessage(JSON.stringify(cmd), "*");
    };
    NEOGAMESDK.makeRawTransaction = function (data, callback) {
        console.log('NEOGAMESDK.makeRawTransaction( data, callback) ');
        if (NEOGAMESDK.is_init === false) {
            console.log("please use init first !");
            return;
        }
        var eventId = NEOGAMESDK.getRandom(NEOGAMESDK.callback_makeRawTransaction);
        NEOGAMESDK.callback_makeRawTransaction[eventId] = callback;
        console.log('NEOGAMESDK.makeRawTransaction.eventId', eventId);
        var cmd = { "cmd": "makeRawTransaction", "eventId": eventId, "data": data };
        parent.window.postMessage(JSON.stringify(cmd), "*");
    };
    NEOGAMESDK.invokescript = function (data, callback) {
        console.log('NEOGAMESDK.invokescript( data, callback ) ');
        if (NEOGAMESDK.is_init === false) {
            console.log("please use init first !");
            return;
        }
        var eventId = NEOGAMESDK.getRandom(NEOGAMESDK.callback_invokescript);
        NEOGAMESDK.callback_invokescript[eventId] = callback;
        console.log('NEOGAMESDK.invokescript.eventId', eventId);
        var cmd = { "cmd": "invokescript", "eventId": eventId, "data": data };
        parent.window.postMessage(JSON.stringify(cmd), "*");
    };
    NEOGAMESDK.getUrlParam = function (name) {
        var reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)"); //构造一个含有目标参数的正则表达式对象
        var r = window.location.search.substr(1).match(reg); //匹配目标参数
        if (r != null) {
            return unescape(r[2]);
        }
        return null; //返回参数值
    };
    NEOGAMESDK.getUrlParams = function () {
        var url = location.search; //获取url中"?"符后的字串  
        var theRequest = new Object();
        if (url.indexOf("?") != -1) {
            var str = url.substr(1);
            var strs = str.split("&");
            for (var i = 0; i < strs.length; i++) {
                theRequest[strs[i].split("=")[0]] = unescape(strs[i].split("=")[1]);
            }
        }
        return theRequest;
    };
    NEOGAMESDK.is_init = false;
    NEOGAMESDK.callback_getUserInfo = {};
    NEOGAMESDK.callback_makeTrans = {};
    NEOGAMESDK.callback_getUTXO = {};
    NEOGAMESDK.callback_makeRawTransaction = {};
    NEOGAMESDK.callback_makeMintTokenTransaction = {};
    NEOGAMESDK.callback_invokescript = {};
    NEOGAMESDK.callback_makeRefundTransaction = {};
    NEOGAMESDK.callback_getSgasBalance = {};
    return NEOGAMESDK;
}());