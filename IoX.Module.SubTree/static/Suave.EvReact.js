var Suave;
(function (Suave) {
    var EvReact;
    (function (EvReact) {
        function remoteCallback(url, callback, rawText) {
            return function (data) {
                var request = new XMLHttpRequest();
                request.open('POST', url, true);
                request.setRequestHeader('Content-Type', 'application/json');
                request.onload = function () {
                    if (request.status < 200 || request.status >= 400)
                        console.error(url, request.status, request.statusText);
                    else if (callback) {
                        var arg = rawText ? request.responseText : JSON.parse(request.responseText);
                        callback(arg);
                    }
                };
                request.onerror = function () { return console.error(url, request.status, request.statusText); };
                request.send(JSON.stringify(data));
            };
        }
        EvReact.remoteCallback = remoteCallback;
        var EventRequest = (function () {
            function EventRequest(url) {
                var _this = this;
                this.listeners = [];
                var dispatch = function (data) {
                    _this.listeners.forEach(function (handler) {
                        handler(data);
                    });
                };
                this.callback = remoteCallback(url, dispatch);
            }
            EventRequest.prototype.trigger = function (data) {
                this.callback(data);
            };
            EventRequest.prototype.addListener = function (cb) {
                this.listeners.push(cb);
            };
            EventRequest.prototype.removeListener = function (cb) {
                var idx = this.listeners.lastIndexOf(cb);
                if (idx != -1)
                    this.listeners.splice(idx, 1);
            };
            return EventRequest;
        }());
        EvReact.EventRequest = EventRequest;
    })(EvReact = Suave.EvReact || (Suave.EvReact = {}));
})(Suave || (Suave = {}));
