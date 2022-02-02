(function (global, factory) {
    typeof exports === 'object' && typeof module !== 'undefined' ? factory(exports, require('vue'), require('vue-class-component')) :
    typeof define === 'function' && define.amd ? define(['exports', 'vue', 'vue-class-component'], factory) :
    (global = typeof globalThis !== 'undefined' ? globalThis : global || self, factory(global.VuePropertyDecorator = {}, global.Vue, global.VueClassComponent));
}(this, (function (exports, vue, vueClassComponent) { 'use strict';

    function _interopDefaultLegacy (e) { return e && typeof e === 'object' && 'default' in e ? e : { 'default': e }; }

    var vue__default = /*#__PURE__*/_interopDefaultLegacy(vue);
    var vueClassComponent__default = /*#__PURE__*/_interopDefaultLegacy(vueClassComponent);

    var __spreadArrays = (undefined && undefined.__spreadArrays) || function () {
        for (var s = 0, i = 0, il = arguments.length; i < il; i++) s += arguments[i].length;
        for (var r = Array(s), k = 0, i = 0; i < il; i++)
            for (var a = arguments[i], j = 0, jl = a.length; j < jl; j++, k++)
                r[k] = a[j];
        return r;
    };
    // Code copied from Vue/src/shared/util.js
    var hyphenateRE = /\B([A-Z])/g;
    var hyphenate = function (str) { return str.replace(hyphenateRE, '-$1').toLowerCase(); };
    /**
     * decorator of an event-emitter function
     * @param  event The name of the event
     * @return MethodDecorator
     */
    function Emit(event) {
        return function (_target, propertyKey, descriptor) {
            var key = hyphenate(propertyKey);
            var original = descriptor.value;
            descriptor.value = function emitter() {
                var _this = this;
                var args = [];
                for (var _i = 0; _i < arguments.length; _i++) {
                    args[_i] = arguments[_i];
                }
                var emit = function (returnValue) {
                    var emitName = event || key;
                    if (returnValue === undefined) {
                        if (args.length === 0) {
                            _this.$emit(emitName);
                        }
                        else if (args.length === 1) {
                            _this.$emit(emitName, args[0]);
                        }
                        else {
                            _this.$emit.apply(_this, __spreadArrays([emitName], args));
                        }
                    }
                    else {
                        args.unshift(returnValue);
                        _this.$emit.apply(_this, __spreadArrays([emitName], args));
                    }
                };
                var returnValue = original.apply(this, args);
                if (isPromise(returnValue)) {
                    returnValue.then(emit);
                }
                else {
                    emit(returnValue);
                }
                return returnValue;
            };
        };
    }
    function isPromise(obj) {
        return obj instanceof Promise || (obj && typeof obj.then === 'function');
    }

    /**
     * decorator of an inject
     * @param from key
     * @return PropertyDecorator
     */
    function Inject(options) {
        return vueClassComponent.createDecorator(function (componentOptions, key) {
            if (typeof componentOptions.inject === 'undefined') {
                componentOptions.inject = {};
            }
            if (!Array.isArray(componentOptions.inject)) {
                componentOptions.inject[key] = options || key;
            }
        });
    }

    function needToProduceProvide(original) {
        return (typeof original !== 'function' ||
            (!original.managed && !original.managedReactive));
    }
    function produceProvide(original) {
        var provide = function () {
            var _this = this;
            var rv = typeof original === 'function' ? original.call(this) : original;
            rv = Object.create(rv || null);
            // set reactive services (propagates previous services if necessary)
            rv[reactiveInjectKey] = Object.create(this[reactiveInjectKey] || {});
            for (var i in provide.managed) {
                rv[provide.managed[i]] = this[i];
            }
            var _loop_1 = function (i) {
                rv[provide.managedReactive[i]] = this_1[i]; // Duplicates the behavior of `@Provide`
                Object.defineProperty(rv[reactiveInjectKey], provide.managedReactive[i], {
                    enumerable: true,
                    configurable: true,
                    get: function () { return _this[i]; },
                });
            };
            var this_1 = this;
            for (var i in provide.managedReactive) {
                _loop_1(i);
            }
            return rv;
        };
        provide.managed = {};
        provide.managedReactive = {};
        return provide;
    }
    /** Used for keying reactive provide/inject properties */
    var reactiveInjectKey = '__reactiveInject__';
    function inheritInjected(componentOptions) {
        // inject parent reactive services (if any)
        if (!Array.isArray(componentOptions.inject)) {
            componentOptions.inject = componentOptions.inject || {};
            componentOptions.inject[reactiveInjectKey] = {
                from: reactiveInjectKey,
                default: {},
            };
        }
    }

    /**
     * decorator of a reactive inject
     * @param from key
     * @return PropertyDecorator
     */
    function InjectReactive(options) {
        return vueClassComponent.createDecorator(function (componentOptions, key) {
            if (typeof componentOptions.inject === 'undefined') {
                componentOptions.inject = {};
            }
            if (!Array.isArray(componentOptions.inject)) {
                var fromKey_1 = !!options ? options.from || options : key;
                var defaultVal_1 = (!!options && options.default) || undefined;
                if (!componentOptions.computed)
                    componentOptions.computed = {};
                componentOptions.computed[key] = function () {
                    var obj = this[reactiveInjectKey];
                    return obj ? obj[fromKey_1] : defaultVal_1;
                };
                componentOptions.inject[reactiveInjectKey] = reactiveInjectKey;
            }
        });
    }

    /** @see {@link https://github.com/vuejs/vue-class-component/blob/master/src/reflect.ts} */
    var reflectMetadataIsSupported = typeof Reflect !== 'undefined' && typeof Reflect.getMetadata !== 'undefined';
    function applyMetadata(options, target, key) {
        if (reflectMetadataIsSupported) {
            if (!Array.isArray(options) &&
                typeof options !== 'function' &&
                !options.hasOwnProperty('type') &&
                typeof options.type === 'undefined') {
                var type = Reflect.getMetadata('design:type', target, key);
                if (type !== Object) {
                    options.type = type;
                }
            }
        }
    }

    /**
     * decorator of model
     * @param  event event name
     * @param options options
     * @return PropertyDecorator
     */
    function Model(event, options) {
        if (options === void 0) { options = {}; }
        return function (target, key) {
            applyMetadata(options, target, key);
            vueClassComponent.createDecorator(function (componentOptions, k) {
                (componentOptions.props || (componentOptions.props = {}))[k] = options;
                componentOptions.model = { prop: k, event: event || k };
            })(target, key);
        };
    }

    /**
     * decorator of synced model and prop
     * @param propName the name to interface with from outside, must be different from decorated property
     * @param  event event name
     * @param options options
     * @return PropertyDecorator
     */
    function ModelSync(propName, event, options) {
        if (options === void 0) { options = {}; }
        return function (target, key) {
            applyMetadata(options, target, key);
            vueClassComponent.createDecorator(function (componentOptions, k) {
                (componentOptions.props || (componentOptions.props = {}))[propName] = options;
                componentOptions.model = { prop: propName, event: event || k };
                (componentOptions.computed || (componentOptions.computed = {}))[k] = {
                    get: function () {
                        return this[propName];
                    },
                    set: function (value) {
                        // @ts-ignore
                        this.$emit(event, value);
                    },
                };
            })(target, key);
        };
    }

    /**
     * decorator of a prop
     * @param  options the options for the prop
     * @return PropertyDecorator | void
     */
    function Prop(options) {
        if (options === void 0) { options = {}; }
        return function (target, key) {
            applyMetadata(options, target, key);
            vueClassComponent.createDecorator(function (componentOptions, k) {
                (componentOptions.props || (componentOptions.props = {}))[k] = options;
            })(target, key);
        };
    }

    /**
     * decorator of a synced prop
     * @param propName the name to interface with from outside, must be different from decorated property
     * @param options the options for the synced prop
     * @return PropertyDecorator | void
     */
    function PropSync(propName, options) {
        if (options === void 0) { options = {}; }
        return function (target, key) {
            applyMetadata(options, target, key);
            vueClassComponent.createDecorator(function (componentOptions, k) {
                (componentOptions.props || (componentOptions.props = {}))[propName] = options;
                (componentOptions.computed || (componentOptions.computed = {}))[k] = {
                    get: function () {
                        return this[propName];
                    },
                    set: function (value) {
                        this.$emit("update:" + propName, value);
                    },
                };
            })(target, key);
        };
    }

    /**
     * decorator of a provide
     * @param key key
     * @return PropertyDecorator | void
     */
    function Provide(key) {
        return vueClassComponent.createDecorator(function (componentOptions, k) {
            var provide = componentOptions.provide;
            inheritInjected(componentOptions);
            if (needToProduceProvide(provide)) {
                provide = componentOptions.provide = produceProvide(provide);
            }
            provide.managed[k] = key || k;
        });
    }

    /**
     * decorator of a reactive provide
     * @param key key
     * @return PropertyDecorator | void
     */
    function ProvideReactive(key) {
        return vueClassComponent.createDecorator(function (componentOptions, k) {
            var provide = componentOptions.provide;
            inheritInjected(componentOptions);
            if (needToProduceProvide(provide)) {
                provide = componentOptions.provide = produceProvide(provide);
            }
            provide.managedReactive[k] = key || k;
        });
    }

    /**
     * decorator of a ref prop
     * @param refKey the ref key defined in template
     */
    function Ref(refKey) {
        return vueClassComponent.createDecorator(function (options, key) {
            options.computed = options.computed || {};
            options.computed[key] = {
                cache: false,
                get: function () {
                    return this.$refs[refKey || key];
                },
            };
        });
    }

    /**
     * decorator for capturings v-model binding to component
     * @param options the options for the prop
     */
    function VModel(options) {
        if (options === void 0) { options = {}; }
        var valueKey = 'value';
        return vueClassComponent.createDecorator(function (componentOptions, key) {
            (componentOptions.props || (componentOptions.props = {}))[valueKey] = options;
            (componentOptions.computed || (componentOptions.computed = {}))[key] = {
                get: function () {
                    return this[valueKey];
                },
                set: function (value) {
                    this.$emit('input', value);
                },
            };
        });
    }

    /**
     * decorator of a watch function
     * @param  path the path or the expression to observe
     * @param  WatchOption
     * @return MethodDecorator
     */
    function Watch(path, options) {
        if (options === void 0) { options = {}; }
        var _a = options.deep, deep = _a === void 0 ? false : _a, _b = options.immediate, immediate = _b === void 0 ? false : _b;
        return vueClassComponent.createDecorator(function (componentOptions, handler) {
            if (typeof componentOptions.watch !== 'object') {
                componentOptions.watch = Object.create(null);
            }
            var watch = componentOptions.watch;
            if (typeof watch[path] === 'object' && !Array.isArray(watch[path])) {
                watch[path] = [watch[path]];
            }
            else if (typeof watch[path] === 'undefined') {
                watch[path] = [];
            }
            watch[path].push({ handler: handler, deep: deep, immediate: immediate });
        });
    }

    Object.defineProperty(exports, 'Vue', {
        enumerable: true,
        get: function () {
            return vue__default['default'];
        }
    });
    Object.defineProperty(exports, 'Component', {
        enumerable: true,
        get: function () {
            return vueClassComponent__default['default'];
        }
    });
    Object.defineProperty(exports, 'Mixins', {
        enumerable: true,
        get: function () {
            return vueClassComponent.mixins;
        }
    });
    exports.Emit = Emit;
    exports.Inject = Inject;
    exports.InjectReactive = InjectReactive;
    exports.Model = Model;
    exports.ModelSync = ModelSync;
    exports.Prop = Prop;
    exports.PropSync = PropSync;
    exports.Provide = Provide;
    exports.ProvideReactive = ProvideReactive;
    exports.Ref = Ref;
    exports.VModel = VModel;
    exports.Watch = Watch;

    Object.defineProperty(exports, '__esModule', { value: true });

})));
