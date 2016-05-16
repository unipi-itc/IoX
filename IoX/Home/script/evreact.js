var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
var collections;
(function (collections) {
    "use strict";
    function arrayIterator(array) {
        var a = array.slice();
        var i = 0;
        return {
            next: function () {
                var done = i === a.length;
                var value;
                if (!done) {
                    value = a[i];
                    i++;
                }
                return { value: value, done: done };
            }
        };
    }
    var Queue = (function () {
        function Queue() {
            this._values = [];
        }
        Object.defineProperty(Queue.prototype, "size", {
            get: function () {
                return this._values.length;
            },
            enumerable: true,
            configurable: true
        });
        Queue.prototype.dequeue = function () {
            return this._values.shift();
        };
        Queue.prototype.enqueue = function (v) {
            this._values.push(v);
        };
        return Queue;
    }());
    collections.Queue = Queue;
    var Map = (function () {
        function Map(equals, hash) {
            this.equals = equals;
            this.hash = hash;
            this._keys = [];
            this._values = [];
        }
        Map.prototype.lookup = function (key) {
            for (var i = 0; i < this._keys.length; i++)
                if (this.equals(key, this._keys[i]))
                    return i;
            return -1;
        };
        Object.defineProperty(Map.prototype, "size", {
            get: function () {
                return this._keys.length;
            },
            enumerable: true,
            configurable: true
        });
        Map.prototype.clear = function () {
            this._keys = [];
            this._values = [];
        };
        Map.prototype.delete = function (key) {
            var idx = this.lookup(key);
            if (idx === -1)
                return false;
            this._keys.splice(idx, 1);
            this._values.splice(idx, 1);
            return true;
        };
        Map.prototype.entries = function () {
            var keys = this._keys.slice();
            var values = this._values.slice();
            var i = 0;
            return {
                next: function () {
                    var done = i === keys.length;
                    var value;
                    if (!done) {
                        value = { key: keys[i], value: values[i] };
                        i++;
                    }
                    return { value: value, done: done };
                }
            };
        };
        Map.prototype.forEach = function (callback, thisArg) {
            // TODO: prevent changes during iteration
            for (var i = 0; i < this._keys.length; i++)
                callback.call(thisArg, this._values[i], this._keys[i], this);
        };
        Map.prototype.get = function (key) {
            var idx = this.lookup(key);
            if (idx === -1)
                return undefined;
            return this._values[idx];
        };
        Map.prototype.has = function (key) {
            var idx = this.lookup(key);
            return idx !== -1;
        };
        Map.prototype.keys = function () {
            return arrayIterator(this._keys);
        };
        Map.prototype.set = function (key, value) {
            var idx = this.lookup(key);
            if (idx === -1) {
                this._keys.push(key);
                this._values.push(value);
            }
            else {
                this._values[idx] = value;
            }
            return this;
        };
        Map.prototype.values = function () {
            return arrayIterator(this._values);
        };
        return Map;
    }());
    collections.Map = Map;
    function imul(a, b) {
        var ah = (a >>> 16) & 0xffff;
        var al = a & 0xffff;
        var bh = (b >>> 16) & 0xffff;
        var bl = b & 0xffff;
        // the shift by 0 fixes the sign on the high part
        // the final |0 converts the unsigned value into a signed value
        return ((al * bl) + (((ah * bl + al * bh) << 16) >>> 0) | 0);
    }
    function setContentEquals(a, b) {
        if (a.size !== b.size || a.currentHash !== b.currentHash)
            return false;
        var iter = a.values();
        var v = iter.next();
        while (!v.done) {
            if (!b.has(v.value))
                return false;
            v = iter.next();
        }
        return true;
    }
    collections.setContentEquals = setContentEquals;
    function setHashFunction(set) {
        var r = set.currentHash ^ 2166136261;
        return imul(r, 16777619);
    }
    collections.setHashFunction = setHashFunction;
    var Set = (function () {
        function Set(equals, hash) {
            this.equals = equals;
            this.hash = hash;
            this._values = [];
            this.currentHash = 0;
        }
        Set.prototype.lookup = function (value) {
            for (var i = 0; i < this._values.length; i++)
                if (this.equals(value, this._values[i]))
                    return i;
            return -1;
        };
        Object.defineProperty(Set.prototype, "size", {
            get: function () {
                return this._values.length;
            },
            enumerable: true,
            configurable: true
        });
        Set.prototype.add = function (value) {
            var h = this.hash(value);
            var idx = this.lookup(value);
            if (idx === -1) {
                this._values.push(value);
                this.currentHash ^= h;
            }
            return this;
        };
        Set.prototype.clear = function () {
            this._values = [];
            this.currentHash = 0;
        };
        Set.prototype.delete = function (value) {
            var h = this.hash(value);
            var idx = this.lookup(value);
            if (idx === -1)
                return false;
            this._values.splice(idx, 1);
            this.currentHash ^= h;
            return true;
        };
        Set.prototype.entries = function () {
            var values = this._values.slice();
            var i = 0;
            return {
                next: function () {
                    var done = i === values.length;
                    var value;
                    if (!done) {
                        value = { key: values[i], value: values[i] };
                        i++;
                    }
                    return { value: value, done: done };
                }
            };
        };
        Set.prototype.forEach = function (callback, thisArg) {
            // TODO: prevent changes during iteration
            for (var i = 0; i < this._values.length; i++)
                callback.call(thisArg, this._values[i], this._values[i], this);
        };
        Set.prototype.has = function (key) {
            var idx = this.lookup(key);
            return idx !== -1;
        };
        Set.prototype.values = function () {
            return arrayIterator(this._values);
        };
        Set.prototype.toString = function () {
            return "[ " + this._values.join(", ") + " ]";
        };
        return Set;
    }());
    collections.Set = Set;
    function swap(a, i, j) {
        var tmp = a[i];
        a[i] = a[j];
        a[j] = tmp;
    }
    var BinaryHeap = (function () {
        function BinaryHeap(less) {
            this.less = less;
            this.values = [];
        }
        Object.defineProperty(BinaryHeap.prototype, "size", {
            get: function () {
                return this.values.length;
            },
            enumerable: true,
            configurable: true
        });
        BinaryHeap.prototype.dequeue = function () {
            var r = this.values[0];
            var n = this.size - 1;
            this.values[0] = this.values[n];
            this.values.pop();
            var i = 0;
            while (true) {
                var left = i << 1;
                var right = left + 1;
                if (right < n &&
                    this.less(this.values[right], this.values[i]) &&
                    this.less(this.values[right], this.values[left])) {
                    swap(this.values, i, right);
                    i = right;
                }
                else if (left < n &&
                    this.less(this.values[left], this.values[i])) {
                    swap(this.values, i, left);
                    i = left;
                }
                else {
                    return r;
                }
            }
        };
        BinaryHeap.prototype.enqueue = function (v) {
            var i = this.size;
            var parent = i >>> 1;
            this.values.push(v);
            while (i > 0 && this.less(this.values[i], this.values[parent])) {
                swap(this.values, i, parent);
                i = parent;
                parent = i >>> 1;
            }
        };
        return BinaryHeap;
    }());
    collections.BinaryHeap = BinaryHeap;
})(collections || (collections = {}));
var evreact;
(function (evreact) {
    "use strict";
    var uid = 0;
    function imul(a, b) {
        var ah = (a >>> 16) & 0xffff;
        var al = a & 0xffff;
        var bh = (b >>> 16) & 0xffff;
        var bl = b & 0xffff;
        // the shift by 0 fixes the sign on the high part
        // the final |0 converts the unsigned value into a signed value
        return ((al * bl) + (((ah * bl + al * bh) << 16) >>> 0) | 0);
    }
    function hashInt(a) {
        var r = a ^ 2166136261;
        return imul(r, 16777619);
    }
    function hashUniqueId(a) {
        return hashInt(a.uid);
    }
    function identicalEquals(a, b) {
        return a === b;
    }
    function comparePriority(a, b) {
        return a.priority < b.priority;
    }
    var PrioritySet = (function () {
        function PrioritySet() {
            this.queue = new collections.BinaryHeap(comparePriority);
            this.set = new collections.Set(identicalEquals, hashUniqueId);
        }
        Object.defineProperty(PrioritySet.prototype, "size", {
            get: function () {
                return this.queue.size;
            },
            enumerable: true,
            configurable: true
        });
        PrioritySet.prototype.clear = function () {
            if (this.size !== 0)
                throw "Illegal state";
            this.set.clear();
        };
        PrioritySet.prototype.dequeue = function () {
            return this.queue.dequeue();
        };
        PrioritySet.prototype.enqueue = function (v) {
            if (!this.set.has(v)) {
                this.set.add(v);
                this.queue.enqueue(v);
            }
        };
        return PrioritySet;
    }());
    var event;
    (function (event) {
        var SingleEventTarget = (function () {
            function SingleEventTarget(name) {
                this.name = name;
                this.uid = uid++;
                this.listeners = new collections.Set(identicalEquals, hashUniqueId);
            }
            SingleEventTarget.prototype.triggerLoop = function (listener) {
                listener.handleEvent(this.args);
            };
            SingleEventTarget.prototype.trigger = function (e) {
                this.args = e;
                this.listeners.forEach(this.triggerLoop, this);
                this.args = null;
            };
            SingleEventTarget.prototype.addHandler = function (handler) {
                this.listeners.add(handler);
            };
            SingleEventTarget.prototype.removeHandler = function (handler) {
                this.listeners.delete(handler);
            };
            SingleEventTarget.prototype.toString = function () {
                return this.name + "[" + this.uid + "]";
            };
            return SingleEventTarget;
        }());
        var EventTargetWrapper = (function () {
            function EventTargetWrapper(target, type) {
                this.target = target;
                this.type = type;
                this.uid = uid++;
            }
            EventTargetWrapper.prototype.addHandler = function (handler) {
                this.target.addEventListener(this.type, handler);
            };
            EventTargetWrapper.prototype.removeHandler = function (handler) {
                this.target.removeEventListener(this.type, handler);
            };
            return EventTargetWrapper;
        }());
        function create(name) {
            return new SingleEventTarget(name);
        }
        event.create = create;
        function wrap(target, type) {
            return new EventTargetWrapper(target, type);
        }
        event.wrap = wrap;
    })(event = evreact.event || (evreact.event = {}));
    var SimpleNet = (function () {
        function SimpleNet(orch, priority) {
            this.orch = orch;
            this.priority = priority;
            this.uid = uid++;
            this.matching = false;
            this.aux = 0;
        }
        SimpleNet.prototype.setMatching = function (v, args) {
            if (v)
                this.parent.notifyMatch(this.aux, args);
            else if (this.matching)
                this.parent.notifyUnmatch(this.aux, args);
            this.matching = v;
        };
        SimpleNet.prototype.start = function (args) {
            throw "Abstract method";
        };
        SimpleNet.prototype.stop = function () {
            throw "Abstract method";
        };
        SimpleNet.prototype.dispose = function () {
            this.stop();
        };
        return SimpleNet;
    }());
    var SealedOrchestrator = (function () {
        function SealedOrchestrator() {
            this.evaluating = false;
            this.dispatchers = new collections.Map(identicalEquals, hashUniqueId);
            this.muxers = new collections.Map(collections.setContentEquals, collections.setHashFunction);
            this.eventQueue = new collections.Queue();
            this.argsQueue = new collections.Queue();
            this.activeGroundTerms = new collections.Set(identicalEquals, hashUniqueId);
            this.activeOperators = new PrioritySet();
            this.disablingOperators = new PrioritySet();
            this.callbacks = new PrioritySet();
        }
        Object.defineProperty(SealedOrchestrator.prototype, "isEmpty", {
            get: function () {
                return this.dispatchers.size == 0 && this.muxers.size == 0;
            },
            enumerable: true,
            configurable: true
        });
        SealedOrchestrator.prototype.enqueueGroundTerm = function (net) {
            this.activeGroundTerms.add(net);
        };
        SealedOrchestrator.prototype.enqueueOpEval = function (net) {
            this.activeOperators.enqueue(net);
        };
        SealedOrchestrator.prototype.enqueueNotifyDisable = function (net) {
            this.disablingOperators.enqueue(net);
        };
        SealedOrchestrator.prototype.enqueueCallback = function (net) {
            this.callbacks.enqueue(net);
        };
        SealedOrchestrator.prototype.dispatcher = function (event) {
            var d = this.dispatchers.get(event);
            if (d === undefined) {
                d = new Dispatcher(this, event);
                this.dispatchers.set(event, d);
            }
            return d;
        };
        SealedOrchestrator.prototype.muxer = function (events) {
            var m = this.muxers.get(events);
            if (m === undefined) {
                m = new Muxer(this, events);
                this.muxers.set(events, m);
            }
            return m;
        };
        SealedOrchestrator.prototype.subscribe = function (events) {
            return new Subscription(this.muxer(events));
        };
        SealedOrchestrator.prototype.unsubscribeDispatcher = function (event) {
            this.dispatchers.delete(event);
        };
        SealedOrchestrator.prototype.unsubscribeMuxer = function (events) {
            this.muxers.delete(events);
        };
        SealedOrchestrator.prototype.enqueueEvent = function (event, args) {
            if (this.evaluating) {
                this.eventQueue.enqueue(event);
                this.argsQueue.enqueue(args);
            }
            else {
                this.evaluating = true;
                while (true) {
                    this.evalEvent(event, args);
                    if (this.eventQueue.size === 0) {
                        this.evaluating = false;
                        return;
                    }
                    event = this.eventQueue.dequeue();
                    args = this.argsQueue.dequeue();
                }
            }
        };
        SealedOrchestrator.prototype.evalEventLoop = function (net) {
            net.eval(this.args);
        };
        SealedOrchestrator.prototype.evalEvent = function (event, args) {
            if (event !== null)
                this.dispatchers.get(event).evalEvent(args);
            this.args = args;
            this.activeGroundTerms.forEach(this.evalEventLoop, this);
            this.activeGroundTerms.clear();
            this.args = null;
            while (this.activeOperators.size !== 0) {
                var net = this.activeOperators.dequeue();
                net.setMatching(net.isMatching(), args);
            }
            this.activeOperators.clear();
            while (this.disablingOperators.size !== 0) {
                var net = this.disablingOperators.dequeue();
                if (net.active.size === 0)
                    net.parent.notifyDeactivation(net.aux, args);
            }
            this.disablingOperators.clear();
            while (this.callbacks.size !== 0) {
                var callbacknet = this.callbacks.dequeue();
                callbacknet.cb(args);
            }
            this.callbacks.clear();
        };
        return SealedOrchestrator;
    }());
    var IEventArgsPair = (function () {
        function IEventArgsPair(event, args) {
            this.event = event;
            this.args = args;
        }
        return IEventArgsPair;
    }());
    var SealedDebugOrchestrator = (function (_super) {
        __extends(SealedDebugOrchestrator, _super);
        function SealedDebugOrchestrator() {
            _super.apply(this, arguments);
            this.onEvent = event.create();
            this.onStepBegin = event.create();
            this.onStepEnd = event.create();
        }
        SealedDebugOrchestrator.prototype.enqueueEvent = function (event, args) {
            this.onEvent.trigger(event);
            _super.prototype.enqueueEvent.call(this, event, args);
        };
        SealedDebugOrchestrator.prototype.evalEvent = function (event, args) {
            this.onStepBegin.trigger(event);
            _super.prototype.evalEvent.call(this, event, args);
            this.onStepEnd.trigger(event);
        };
        return SealedDebugOrchestrator;
    }(SealedOrchestrator));
    var Dispatcher = (function () {
        function Dispatcher(orch, event) {
            this.orch = orch;
            this.event = event;
            this.uid = uid++;
            this.active = new collections.Set(identicalEquals, hashUniqueId);
            this.inactive = new collections.Set(identicalEquals, hashUniqueId);
        }
        Dispatcher.prototype.handleEvent = function (args) {
            this.orch.enqueueEvent(this.event, args);
        };
        Dispatcher.prototype.evalEventLoop = function (m) {
            if (m.evalEvent(this, this.args))
                this.deactivate.push(m);
        };
        Dispatcher.prototype.evalEvent = function (args) {
            this.args = args;
            this.deactivate = [];
            this.active.forEach(this.evalEventLoop, this);
            for (var i = 0; i < this.deactivate.length; i++) {
                this.active.delete(this.deactivate[i]);
                this.inactive.add(this.deactivate[i]);
            }
            this.deactivate = null;
            this.args = null;
            if (this.active.size === 0)
                this.event.removeHandler(this);
        };
        Dispatcher.prototype.attach = function (mux) {
            if (this.active.size === 0)
                this.event.addHandler(this);
            this.inactive.delete(mux);
            this.active.add(mux);
        };
        Dispatcher.prototype.detach = function (mux) {
            this.inactive.delete(mux);
            this.active.delete(mux);
            if (this.active.size === 0) {
                this.event.removeHandler(this);
                if (this.inactive.size === 0)
                    this.orch.unsubscribeDispatcher(this.event);
            }
        };
        Dispatcher.prototype.toString = function () {
            return this.event.toString();
        };
        return Dispatcher;
    }());
    var Muxer = (function () {
        function Muxer(orch, events) {
            this.orch = orch;
            this.events = events;
            this.uid = uid++;
            this.activeSubscriptions = new collections.Set(identicalEquals, hashUniqueId);
            this.inactiveSubscriptions = new collections.Set(identicalEquals, hashUniqueId);
            this.enabledDispatchers = new collections.Set(identicalEquals, hashUniqueId);
            this.disabledDispatchers = new collections.Set(identicalEquals, hashUniqueId);
            this.events.forEach(this.constructorLoop, this);
        }
        Muxer.prototype.constructorLoop = function (e) {
            this.disabledDispatchers.add(this.orch.dispatcher(e));
        };
        Muxer.prototype.evalEventLoop = function (s) {
            s.evalEventFun(s.evalEventObj, this.args);
        };
        Muxer.prototype.evalEvent = function (dispatcher, args) {
            var r = this.activeSubscriptions.size === 0;
            if (r) {
                this.enabledDispatchers.delete(dispatcher);
                this.disabledDispatchers.add(dispatcher);
            }
            else {
                this.args = args;
                this.activeSubscriptions.forEach(this.evalEventLoop, this);
                this.args = null;
            }
            return r;
        };
        Muxer.prototype.enableLoop = function (d) {
            d.attach(this);
            this.enabledDispatchers.add(d);
        };
        Muxer.prototype.enable = function (subscription) {
            if (this.activeSubscriptions.size === 0) {
                this.disabledDispatchers.forEach(this.enableLoop, this);
                this.disabledDispatchers.clear();
            }
            this.inactiveSubscriptions.delete(subscription);
            this.activeSubscriptions.add(subscription);
        };
        Muxer.prototype.disable = function (subscription) {
            this.activeSubscriptions.delete(subscription);
            this.inactiveSubscriptions.add(subscription);
        };
        Muxer.prototype.unsubscribeLoop = function (d) {
            d.detach(this);
        };
        Muxer.prototype.unsubscribe = function (subscription) {
            this.activeSubscriptions.delete(subscription);
            this.inactiveSubscriptions.delete(subscription);
            if (this.activeSubscriptions.size === 0 &&
                this.inactiveSubscriptions.size === 0) {
                this.enabledDispatchers.forEach(this.unsubscribeLoop, this);
                this.disabledDispatchers.forEach(this.unsubscribeLoop, this);
                this.orch.unsubscribeMuxer(this.events);
            }
        };
        Muxer.prototype.toString = function () {
            return this.events.toString();
        };
        return Muxer;
    }());
    var Subscription = (function () {
        function Subscription(mux) {
            this.mux = mux;
            this.uid = uid++;
        }
        Subscription.prototype.enable = function () {
            this.mux.enable(this);
        };
        Subscription.prototype.disable = function () {
            this.mux.disable(this);
        };
        Subscription.prototype.dispose = function () {
            this.mux.unsubscribe(this);
        };
        Subscription.prototype.toString = function () {
            return this.mux.toString();
        };
        return Subscription;
    }());
    var UnaryOperatorNet = (function (_super) {
        __extends(UnaryOperatorNet, _super);
        function UnaryOperatorNet(orch, subnet) {
            _super.call(this, orch, 1 + subnet.priority);
            this.subnet = subnet;
            subnet.parent = this;
        }
        UnaryOperatorNet.prototype.start = function (args) {
            this.subnet.start(args);
        };
        UnaryOperatorNet.prototype.stop = function () {
            this.subnet.stop();
        };
        UnaryOperatorNet.prototype.notifyDeactivation = function (aux, args) {
            this.parent.notifyDeactivation(this.aux, args);
        };
        UnaryOperatorNet.prototype.notifyMatch = function (aux, args) {
            this.setMatching(true, args);
        };
        UnaryOperatorNet.prototype.notifyUnmatch = function (aux, args) {
            this.setMatching(false, args);
        };
        return UnaryOperatorNet;
    }(SimpleNet));
    var CallbackNet = (function (_super) {
        __extends(CallbackNet, _super);
        function CallbackNet(orch, subnet, cb) {
            _super.call(this, orch, subnet);
            this.cb = cb;
        }
        return CallbackNet;
    }(UnaryOperatorNet));
    var ReactNet = (function (_super) {
        __extends(ReactNet, _super);
        function ReactNet(orch, subnet, cb) {
            _super.call(this, orch, subnet, cb);
        }
        ReactNet.prototype.notifyMatch = function (aux, args) {
            this.orch.enqueueCallback(this);
            this.setMatching(true, args);
        };
        ReactNet.prototype.toString = function () {
            return "(" + this.subnet + ") |-> ...";
        };
        return ReactNet;
    }(CallbackNet));
    var FinallyNet = (function (_super) {
        __extends(FinallyNet, _super);
        function FinallyNet(orch, subnet, cb) {
            _super.call(this, orch, subnet, cb);
        }
        FinallyNet.prototype.notifyDeactivation = function (aux, args) {
            this.orch.enqueueCallback(this);
        };
        FinallyNet.prototype.toString = function () {
            return "(" + this.subnet + ") |=> ...";
        };
        return FinallyNet;
    }(CallbackNet));
    var IterNet = (function (_super) {
        __extends(IterNet, _super);
        function IterNet() {
            _super.apply(this, arguments);
        }
        IterNet.prototype.notifyMatch = function (aux, args) {
            this.start(args);
            this.setMatching(true, args);
        };
        IterNet.prototype.toString = function () {
            return "+(" + this.subnet + ")";
        };
        return IterNet;
    }(UnaryOperatorNet));
    var GroundTermNet = (function (_super) {
        __extends(GroundTermNet, _super);
        function GroundTermNet(orch, predicate, e, bound) {
            _super.call(this, orch, 0);
            this.predicate = predicate;
            this.active = false;
            this.successful = false;
            var eset = new collections.Set(identicalEquals, hashUniqueId);
            eset.add(e);
            this.pos = orch.subscribe(eset);
            this.neg = orch.subscribe(bound);
            this.pos.evalEventFun = this.posCb;
            this.neg.evalEventFun = this.negCb;
            this.pos.evalEventObj = this;
            this.neg.evalEventObj = this;
        }
        GroundTermNet.prototype.posCb = function (o, args) {
            if (o.predicate(args)) {
                o.successful = true;
                o.orch.enqueueGroundTerm(o);
            }
        };
        GroundTermNet.prototype.negCb = function (o, args) {
            o.orch.enqueueGroundTerm(o);
        };
        GroundTermNet.prototype.initialized = function () {
            return this.pos !== null;
        };
        GroundTermNet.prototype.start = function (args) {
            this.successful = false;
            this.setMatching(this.successful, args);
            if (!this.active) {
                this.pos.enable();
                this.neg.enable();
                this.active = true;
            }
        };
        GroundTermNet.prototype.eval = function (args) {
            this.active = false;
            this.pos.disable();
            this.neg.disable();
            this.setMatching(this.successful, args);
            if (!this.active)
                this.parent.notifyDeactivation(this.aux, args);
        };
        GroundTermNet.prototype.stop = function () {
            if (this.initialized()) {
                this.pos.dispose();
                this.neg.dispose();
                this.pos = null;
                this.neg = null;
            }
        };
        GroundTermNet.prototype.toString = function () {
            var c = this.active ? "." : "";
            return c + this.pos + "/" + this.neg;
        };
        return GroundTermNet;
    }(SimpleNet));
    function maxPriority(nets) {
        var r = -1;
        for (var i = 0; i < nets.length; i++) {
            var p = nets[i].priority;
            if (r < p)
                r = p;
        }
        return r;
    }
    var OperatorNet = (function (_super) {
        __extends(OperatorNet, _super);
        function OperatorNet(orch, subnets) {
            _super.call(this, orch, 1 + maxPriority(subnets));
            this.subnets = subnets;
            this.active = new collections.Set(identicalEquals, hashInt);
            for (var i = 0; i < subnets.length; i++) {
                var n = subnets[i];
                n.parent = this;
                n.aux = i;
            }
        }
        OperatorNet.prototype.substart = function (i, args) {
            this.active.add(i);
            this.subnets[i].start(args);
        };
        OperatorNet.prototype.stop = function () {
            for (var i = 0; i < this.subnets.length; i++)
                this.subnets[i].stop();
        };
        OperatorNet.prototype.notifyDeactivation = function (aux, args) {
            this.active.delete(aux);
            if (this.active.size === 0)
                this.orch.enqueueNotifyDisable(this);
        };
        OperatorNet.prototype.isMatching = function () {
            throw "Abstract method";
        };
        OperatorNet.prototype.notifyMatch = function (aux, args) {
            throw "Abstract method";
        };
        OperatorNet.prototype.notifyUnmatch = function (aux, args) {
            throw "Abstract method";
        };
        return OperatorNet;
    }(SimpleNet));
    function opString(operands, op, empty) {
        if (operands.length === 0)
            return empty;
        return "(" + operands.join(") " + op + " (") + ")";
    }
    var CatNet = (function (_super) {
        __extends(CatNet, _super);
        function CatNet() {
            _super.apply(this, arguments);
            this.submatching = false;
        }
        CatNet.prototype.isMatching = function () {
            return this.submatching;
        };
        CatNet.prototype.start = function (args) {
            if (this.subnets.length !== 0) {
                this.submatching = false;
                this.substart(0, args);
            }
            else {
                this.parent.notifyDeactivation(this.aux, args);
            }
        };
        CatNet.prototype.notifyMatch = function (aux, args) {
            var next = aux + 1;
            if (next === this.subnets.length) {
                this.submatching = true;
                this.orch.enqueueOpEval(this);
            }
            else {
                this.substart(next, args);
            }
        };
        CatNet.prototype.notifyUnmatch = function (aux) {
            var next = aux + 1;
            if (next === this.subnets.length) {
                this.submatching = false;
                this.orch.enqueueOpEval(this);
            }
        };
        CatNet.prototype.toString = function () {
            return opString(this.subnets, "-", "nil");
        };
        return CatNet;
    }(OperatorNet));
    var CommutativeOperatorNet = (function (_super) {
        __extends(CommutativeOperatorNet, _super);
        function CommutativeOperatorNet(orch, subnets) {
            _super.call(this, orch, subnets);
            this.submatching = new collections.Set(identicalEquals, hashInt);
        }
        CommutativeOperatorNet.prototype.start = function (args) {
            if (!this.active.has(-1)) {
                if (this.subnets.length === 0)
                    this.setMatching(this.isMatching(), args);
                else
                    for (var i = 0; i < this.subnets.length; i++)
                        this.substart(i, args);
                this.notifyDeactivation(-1, args);
            }
        };
        CommutativeOperatorNet.prototype.notifyMatch = function (aux) {
            this.submatching.add(aux);
            if (this.isMatching())
                this.orch.enqueueOpEval(this);
        };
        CommutativeOperatorNet.prototype.notifyUnmatch = function (aux) {
            this.submatching.delete(aux);
            if (!this.isMatching())
                this.orch.enqueueOpEval(this);
        };
        return CommutativeOperatorNet;
    }(OperatorNet));
    var AllNet = (function (_super) {
        __extends(AllNet, _super);
        function AllNet() {
            _super.apply(this, arguments);
        }
        AllNet.prototype.isMatching = function () {
            return this.submatching.size === this.subnets.length;
        };
        AllNet.prototype.toString = function () {
            return opString(this.subnets, "&&&", "epsilon");
        };
        return AllNet;
    }(CommutativeOperatorNet));
    var AnyNet = (function (_super) {
        __extends(AnyNet, _super);
        function AnyNet() {
            _super.apply(this, arguments);
        }
        AnyNet.prototype.isMatching = function () {
            return this.submatching.size !== 0;
        };
        AnyNet.prototype.toString = function () {
            return opString(this.subnets, "|||", "nil");
        };
        return AnyNet;
    }(CommutativeOperatorNet));
    var orchestrator;
    (function (orchestrator) {
        function create() {
            return new SealedOrchestrator();
        }
        orchestrator.create = create;
        function createDebug() {
            return new SealedDebugOrchestrator();
        }
        orchestrator.createDebug = createDebug;
    })(orchestrator = evreact.orchestrator || (evreact.orchestrator = {}));
    var expr;
    (function (expr_1) {
        function compileAll(orch, bounds, exprs) {
            var nets = new Array(exprs.length);
            for (var i = 0; i < exprs.length; i++)
                nets[i] = exprs[i].compile(orch, bounds);
            return nets;
        }
        var SimpleExpr = (function () {
            function SimpleExpr(event, pred) {
                this.event = event;
                this.pred = pred;
            }
            SimpleExpr.prototype.compile = function (orch, bounds) {
                return new GroundTermNet(orch, this.pred, this.event, bounds);
            };
            return SimpleExpr;
        }());
        var AllExpr = (function () {
            function AllExpr(subexprs) {
                this.subexprs = subexprs;
            }
            AllExpr.prototype.compile = function (orch, bounds) {
                return new AllNet(orch, compileAll(orch, bounds, this.subexprs));
            };
            return AllExpr;
        }());
        var AnyExpr = (function () {
            function AnyExpr(subexprs) {
                this.subexprs = subexprs;
            }
            AnyExpr.prototype.compile = function (orch, bounds) {
                return new AnyNet(orch, compileAll(orch, bounds, this.subexprs));
            };
            return AnyExpr;
        }());
        var CatExpr = (function () {
            function CatExpr(subexprs) {
                this.subexprs = subexprs;
            }
            CatExpr.prototype.compile = function (orch, bounds) {
                return new CatNet(orch, compileAll(orch, bounds, this.subexprs));
            };
            return CatExpr;
        }());
        var IterExpr = (function () {
            function IterExpr(subexpr) {
                this.subexpr = subexpr;
            }
            IterExpr.prototype.compile = function (orch, bounds) {
                return new IterNet(orch, this.subexpr.compile(orch, bounds));
            };
            return IterExpr;
        }());
        var ReactExpr = (function () {
            function ReactExpr(subexpr, reaction) {
                this.subexpr = subexpr;
                this.reaction = reaction;
            }
            ReactExpr.prototype.compile = function (orch, bounds) {
                return new ReactNet(orch, this.subexpr.compile(orch, bounds), this.reaction);
            };
            return ReactExpr;
        }());
        var FinallyExpr = (function () {
            function FinallyExpr(subexpr, reaction) {
                this.subexpr = subexpr;
                this.reaction = reaction;
            }
            FinallyExpr.prototype.compile = function (orch, bounds) {
                return new FinallyNet(orch, this.subexpr.compile(orch, bounds), this.reaction);
            };
            return FinallyExpr;
        }());
        var RestrictExpr = (function () {
            function RestrictExpr(subexpr, bounds) {
                this.subexpr = subexpr;
                this.bounds = bounds;
            }
            RestrictExpr.prototype.compileLoop = function (e) {
                this.tempBounds.add(e);
            };
            RestrictExpr.prototype.compile = function (orch, bounds) {
                this.tempBounds = new collections.Set(identicalEquals, hashUniqueId);
                for (var i = 0; i < this.bounds.length; i++)
                    this.tempBounds.add(this.bounds[i]);
                bounds.forEach(this.compileLoop, this);
                var net = this.subexpr.compile(orch, this.tempBounds);
                this.tempBounds = null;
                return net;
            };
            return RestrictExpr;
        }());
        function trueP(ignored) {
            return true;
        }
        function simple(e) {
            return cond(e, trueP);
        }
        expr_1.simple = simple;
        function cond(e, pred) {
            return new SimpleExpr(e, pred);
        }
        expr_1.cond = cond;
        function all(subexprs) {
            return new AllExpr(subexprs);
        }
        expr_1.all = all;
        function any(subexprs) {
            return new AnyExpr(subexprs);
        }
        expr_1.any = any;
        function cat(subexprs) {
            return new CatExpr(subexprs);
        }
        expr_1.cat = cat;
        function iter(subexpr) {
            return new IterExpr(subexpr);
        }
        expr_1.iter = iter;
        function restrict(subexpr, neg) {
            return new RestrictExpr(subexpr, neg);
        }
        expr_1.restrict = restrict;
        function react(subexpr, reaction) {
            return new ReactExpr(subexpr, reaction);
        }
        expr_1.react = react;
        function finallyDo(subexpr, reaction) {
            return new FinallyExpr(subexpr, reaction);
        }
        expr_1.finallyDo = finallyDo;
        function ignoreAux(aux) {
        }
        function start(args, orch, expr) {
            var iexpr = expr;
            var iorch = orch;
            var net = iexpr.compile(iorch, new collections.Set(identicalEquals, hashUniqueId));
            net.parent = {
                notifyDeactivation: function (aux) { return net.stop(); },
                notifyMatch: ignoreAux,
                notifyUnmatch: ignoreAux
            };
            net.aux = 0;
            net.start(args);
            return net;
        }
        expr_1.start = start;
        function stop(net) {
            net.dispose();
        }
        expr_1.stop = stop;
    })(expr = evreact.expr || (evreact.expr = {}));
})(evreact || (evreact = {}));
