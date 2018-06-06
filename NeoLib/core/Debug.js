
var Debug = function() {
}

Debug.output = function(type, msg) {
    switch (type) {
        case Debug.CLEAR: {
            this.strLog = "";
            break;
        }
        case Debug.FUN: {
            this.strLog += '<font color="#0000CC">fun:</font> <font color="#0000EE">' + msg + '</font><br/>';
            break;
        }
        case Debug.INFO: {
            this.strLog += '<font color="#008800">info:</font> ' + msg + '<br/>';
            break;
        }
        case Debug.ERR: {
            this.strLog += '<font color="#DD1100">err:</font> ' + msg + '<br/>';
            break;
        }
    }
    $("#output").html(this.strLog);
}

Debug.CLEAR = 0;
Debug.FUN = 1;
Debug.INFO = 2;
Debug.ERR = 3;
Debug.strLog = "";
