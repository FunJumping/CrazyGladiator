
var Helper = (function () {
	function Helper() {
	}
	
	Helper.Bytes2String = function (_arr) {
		var UTF = '';
		for (var i = 0; i < _arr.length; i++) {
			var one = _arr[i].toString(2), v = one.match(/^1+?(?=0)/);
			if (v && one.length == 8) {
				var bytesLength = v[0].length;
				var store = _arr[i].toString(2).slice(7 - bytesLength);
				for (var st = 1; st < bytesLength; st++) {
					store += _arr[st + i].toString(2).slice(2);
				}
				UTF += String.fromCharCode(parseInt(store, 2));
				i += bytesLength - 1;
			}
			else {
				UTF += String.fromCharCode(_arr[i]);
			}
		}
		return UTF;
	};

	return Helper;
}());

function toHexStr(str, allowZero = true) {
    var hex = "";
    for (var i = 0; i < str.length; i++) {
        var code = str.charCodeAt(i);
        if (code === 0) {
            if (allowZero) {
                hex += '00';
            }
            else {
                break;
            }
        }
        else {
            var n = code.toString(16);
            hex += n.length < 2 ? '0' + n : n;
        }
    }
    return hex;
};

function bytes2Hex(bytes, allowZero = true) {
    var hex = "";
    for (var i = 0; i < bytes.length; i++) {
        var code = bytes[i];
        if (code === 0) {
            if (allowZero) {
                hex += '00';
            }
            else {
                break;
            }
        }
        else {
            var n = code.toString(16);
            hex += n.length < 2 ? '0' + n : n;
        }
    }
    return hex;
};

String.prototype.hexToBytes = function () {
    if ((this.length & 1) != 0) {
        return this;
        //throw new RangeError();
    }
    var str = this;
    if (this.length >= 2 && this[0] == '0' && this[1] == 'x')
        str = this.substr(2);
    var bytes = new Uint8Array(str.length / 2);
    for (var i = 0; i < bytes.length; i++) {
        bytes[i] = parseInt(str.substr(i * 2, 2), 16);
    }
    return bytes;
};

var WWW = (function () {
	function WWW() {
	}
	
	WWW.makeRpcPostBody = function(method) {
		var body = {};
		body["jsonrpc"] = "2.0";
		body["id"] = 1;
		body["method"] = method;
		var params = [];
		for (var i = 1; i < arguments.length; i++) {
			params.push(arguments[i]);
		}
		body["params"] = params;
		return body;
	}
	
	return WWW;
}());
