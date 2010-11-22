/*
 * jLinq 2.2.1 : 9-12-2009
 * ---------------------------------
 * Hugo Bonacci (webdev_hb@yahoo.com)
 * www.hugoware.net
 * License: Attribution-Share Alike
 * http://creativecommons.org/licenses/by-sa/3.0/us/
 */
 
var jLinq;
(function() {

	//contains functionality to create a new library for jLinq
	var library = function(settings) {
		var _jLinq = this;
		var _p = {};
		_p.settings = settings;
		
		//locks a jLinq object entirely
		_jLinq.finish = function(lock) {
			_jLinq.finish = null;
			_p.loaded = true;
			_p.lock = lock;
		};
		
		//utilities
		_p.util = {
			format:function(msg,args) {
				return msg.toString().replace(/%[0-9]+%/gi, function(match) {
					var index = parseInt(match.replace(/%/gi, ""));
					return args[index];
				});
			},
			allValues:function(list) {
			    var actual = [];
			    for(var i = 0; i < list.length; i++) {
			        if (list[i] == null) { return actual; }
			        actual.push(list[i]);
			    }
			    return actual;
			},
			trim:function(val) {
				if (val == null) { return ""; }
				return val.toString().replace(/^\s+|\s+$/, "");				
			},
			empty:function(list) {
				for(var i = 0; i < list.length; i++) {
					if (list[i]) { return false; }
				}
				return true;
			},
			type:function(val) {
				
				//if null, return null
				if (val == null) { return "null"; }
				if (val == undefined) { return "null"; }
				
				//check each type that has been saved
				for (var item in _p.types) {
					try {
						if (_p.types[item](val)) { return item; }
					}
					catch (e) {
						//no real concern here
					}					
				}
				
				//if nothing was found, just return the typeof value
				return (typeof(val)).toString().toLowerCase();
				
			},
			when:function(val, actions) {
				var type = _p.util.type(val);
				if (!actions[type]) { 
					if (actions.empty && (val == null || val == undefined)) { return actions.empty(val); }
					if (actions.other) { return actions.other(val); }
					return false; 
				}
				try {
					return actions[type](val);								
				}
				catch (e) {
					return false;
				}
			},
			as:function(val, actions) {
				var type = _p.util.type(val);
				if (!actions[type]) { 
					if (actions.empty && (val == null || val == undefined)) { return actions.empty(val); }
					if (actions.other) { return actions.other(val); }
					return null; 
				}
				try {
					return actions[type](val);								
				}
				catch (e) {
					return null;
				}
			},
			each:function(array, action) { 
				var results = [];
				for (var i = 0; i < array.length; i++) {
					try {
						results.push(action(array[i], i)); 
					}
					catch(e) {
						results.push(e);
					}
				}
				return results;
			},
			clone: function(obj) {	
                function gen(){};
                gen.prototype = obj;
                return new gen();
			}
		};
		
		//Types to evaluate for using .when()
		_jLinq.addType = function(name, compare) {		
			name = _p.util.trim(name).toLowerCase();
			_p.types[name] = compare;	
		};
		_jLinq.removeType = function(name) {
			name = _p.util.trim(name).toLowerCase();
			_p.types[name] = function() { return false; };
		};		
		_p.types = settings.types ? settings.types : {};
		
		//Extends functionality onto the jLinq object
		//===============================================================================
		_jLinq.extend = function(params) {
			params.name = _p.util.trim(params.name);
			params.namespace = _p.util.trim(params.namespace);
		
			//check if this is locked
			if (_p.extend.hasCmd(params)) { 
				if (_p.lock) { throw "Exception: Library is locked."; }
				_p.extend.removeCmd(params);
			}
			
			//add this command to the list
			_p.extend.addCmd(params);
		
			//source extensions are evaluated immediately
			if (params.type.match(/source/i)) {
			
				//determine the correct source
				if (params.namespace && !_jLinq[params.namespace]) { _jLinq[params.namespace] = {}; }
				var target = params.namespace == "" ? _jLinq : _jLinq[params.namespace];
				
				//apply this command				
				target[params.name] = function(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25) {
					
					//prepare the command for use
					var results = params.method({
						helper:_p.util
					}, v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25);
					
					//make sure this is an array
					if (_p.util.type(results) != "array") {
						throw "Exception: A 'Source' extension must return an array for a jLinq query.";
					}
					
					//if this should generate a source
					return new _p.query(results);
						
				};
			
			}	
		
		};		
		_p.extend = {
			cmd:[],
			hasCmd:function(params) {			
				return (_p.extend.findCmd(params) != null);
			},
			addCmd:function(params) {		
				_p.extend.removeCmd(params);
				_p.extend.cmd.push(params);
			},
			removeCmd:function(params) {
				var index = _p.extend.findCmd(params);
				if (index) { 
					_p.extend.cmd.splice(index, 1); 
				}
				
			},
			findCmd:function(params) {
				for(var i = 0; i < _p.extend.cmd.length; i++) {
					var method = _p.extend.cmd[i];
					if (method.name == params.name && method.namespace == params.namespace) {
						return i;
					}
				}
				return null;
			}			
		};
		
		//return a list of the commands available
		_jLinq.showCommands = function() {
		    return _p.util.clone(_p.extend.cmd);
		};

		//add all of the current extension methods		
		for (var item in settings.extend) {
			_jLinq.extend(settings.extend[item]);
		}
		
		//Generates the actual query for use
		//===============================================================================
		_p.query = function(data) {
			var _query = this;
			
			//duplicate this object
			data = _p.util.clone(data);
					
			//State of the query
			var _s = {};
			_s.state = {
				properties:true,
				lastCommand:null,
				lastField:null,
				lastCommandName:null,
				paramCount:0,
				ignoreCase:true,
				or:false,
				not:false,
				data:data,
				useProperties:false,
				operator:"",
				debug:{
					onEvent:function(msg) {  },
					log:function(msg,args) {
						_s.state.debug.onEvent(_p.util.format(msg,args));
					}
				}
			};
			
			//determine the type of data this is
			if (data == null) { return null; }
			if (_p.util.type(data) == "array" && data.length > 0) {
				_s.state.useProperties = (_p.util.type(data[0]) == "object");
			}
			
			//Query evaluation
			_s.query = {
				cache:[],
				str:[],
				appendCmd:function(action) {
					_s.query.cache.push(action);
					
					//set the true or false value
					var not = _s.state.not ? "!" : "";
					
					//clear up an existing 
					if (_s.query.cache.length == 0) {
						_s.state.operator = "";
					}
					
					//undo any state changes
					_s.state.or = false;
					_s.state.not = false;
					
					//append the items
					_s.query.str.push([_s.state.operator, "(", not, "(_s.query.cache[", (_s.query.cache.length - 1), "](record)))"].join(""));

					//update the operator as needed
					_s.state.operator = "&&";				
					
				},
				select:function() {
				
					//if there hasn't been a command, return everything
					if (_s.query.str.length == 0) { 
						return {
							selected:_s.state.data,
							remaining: []
						};
					}
				
					//get the query string
					var query;
					eval(["query = function(record) {" + " return (", _s.query.str.join(""), "); };" ].join(""));
				
					//eval and select each result
					var selected = []; var remaining = [];
					for(var i = 0; i < _s.state.data.length; i++) {
						var item = _s.state.data[i];
						try {						
							if (query(item)) {
								selected.push(item);
							}
							else {
								remaining.push(item);
							}
						}
						catch (e) {
							
							_s.state.debug.log("Exception when evaluating the query for selection: %0%. query: %1%", [e,query]);
							remaining.push(item);	
						}
					}
					
					//return the final results
					return {
						selected:selected,
						remaining:remaining
					};					
					
				},
				prepCmd:function(params, args) {
				
					//prepare this command
					//set the known data
					_s.state.lastCommand = params.command;
					_s.state.paramCount = params.count;
					_s.state.lastCommandName = params.name;
		
					//start by clearing any null data
					var values = []; var found = false;
					for (var i = args.length; i-- > 0;) {
						if (!args[i] && !found) { continue; }
						found = true;
						values.push(args[i]);
					}
					values.reverse();
		
					//detetmine if a field was set in this
					//if the data list count is greater than
					//the required parameters, then the first
					//param should be the name of the field            
					if (_s.state.useProperties && values.length == params.count + 1) {
						_s.state.lastField = values.shift();
					}
					
					//return an object to execute with
					return {
						arg: values,
						field: _s.state.lastField
					};
				},
				repeatCmd:function(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25) {
					if (_s.helper.empty([v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25])) { return; }				
					_s.state.lastCommand(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25);				
				},
				performSort:function(records, field, desc) {
				    records.sort(function(a,b) {
			            a = a[field];
			            b = b[field];
                        return (a < b) ? -1 : (a > b) ? 1 : 0;
                    });
                    if (desc) { records.reverse(); }
				}
			};
			
			//Helper methods
			_s.helper = {
				toRegex:function(val) {
					if (val == null) { return ""; }
					return val.toString().replace(
						/\*|\(|\)|\\|\/|\?|\.|\*|\+|\<|\>|\[|\]|\=|\&|\$|\^/gi, 
						function(match) { return ("\\" + match); }
						);
				
				},
				getNumericValue:function(obj) {
					if (obj.length) { return obj.length; }
					return obj;
				},
				trim:_p.util.trim,
				match:function(val,exp) {
					if (!(val && exp)) { return false; }
					if (_s.helper.type(exp) == "regexp") { exp = exp.source; }					
					exp =  new RegExp(exp, "g"+(_s.state.ignoreCase?"i":""));					
					return (val.match(exp));
				},
				propsEqual:function(val1, val2) {
					if (val1 == null && val2 == null) { return true; }
					if (val1 == null || val2 == null) { return false; }
					for(var name in val1) {
						if (val2[name] == undefined) { return false; }
						if (!_s.helper.equals(val1[name], val2[name])) { return false; }
					}
					return true;
				},
				equals:function(val1,val2) {
					try {
					
						//check for null values first
						if (val1 == null && val2 == null) { return true; }
						if ((val1 == null && val2) || (val1 && val2 == null)) { return false; }
						
						//if this is a string, check for case
						var val1Type = _s.helper.type(val1);
						var val2Type = _s.helper.type(val2);
						if (val1Type != val2Type) {
							return false;
						}
						if (val1Type == "string" && val2Type == "string") {
							return _s.helper.match(val1, "^"+val2+"$");
						}
						if (val1Type == "string" &&  val2Type == "regexp") {
							return _s.helper.match(val1, val2);
						}
						if (val1Type == "number" || val1Type == "bool") {
							return (val1 == val2);
						}
						else if (val1Type == "array" || val1Type == "object") {
							if (val1.length != val2.length) { return false; }
							for (var i = 0; i < val1.length; i++) {
								if (!_s.helper.equals(val1[i], val2[i])) { return false; }
							}
							return true;
						}
						else {
							return (val1 == val2);
						}						
					}
					catch (e) {
						return false;
					}
				},
				allEqual:function(val1,val2) {
					if (_p.helper.type(val1) != "array") { val1 = [val1]; }
					for (var item in val1) {
						if (!_p.helper.equals(val1[item], val2)) { return false; }
					}
					return true;
				},
				anyEqual:function(val1,val2) {
					if (_p.helper.type(val1) != "array") { val1 = [val1]; }
					for (var item in val1) {
						if (!_p.helper.equals(val1[item], val2)) { return true; }
					}
					return false;
				},
				sort:function(records, sorting, desc) {
				
				    //if no sorting was provided
				    if (sorting == null) {				    
						records.sort();
						if (desc) { query.state.data.reverse(); }
						return records;
				    }				
				
				    //recursively handle sorting
					var index = 0;
					var doSort = function(records) {  
					
					    //clone to fix a BIZZARE IE7 bug                      
                        records = _p.util.clone(records);
                        
					    var field = sorting[index].field;
					    var desc = sorting[index].desc;
					    
                        //if at the end, just sort it
                        if (index == sorting.length - 1) {
                            _s.query.performSort(records, field, desc);
                            return records;
                        };                        
        	
                        //increment forward to the next command
                        _s.query.performSort(records, field, desc);
                        var dist = _s.helper.distinct(records, field);
                        index++;
						
                        //sort and gather values - call _doSort again if 
                        //to check for futher commands
                        var results = [];
                        for (var j = 0; j < dist.length; j++) {
                            var sorted = doSort(dist[j].items);
                            for (var k = 0; k < sorted.length; k++) {
                                results.push(sorted[k]);
                            }
                        };
        	
                        //return the results for this section
                        return results;
					
					};
				    return doSort(_s.state.data);
				    
				    
				},
				distinct:function(records, field) {
				    var dist = [];
				    for (var i = 0; i < records.length; i++) {
				        var val = records[i];
				        var key = (field != null) ? eval(["(val.", field, ")"].join("")) : val;
				        
				        //check if the value exists yet
						var added = false;						
				        for (var item in dist) {
							if (dist[item].key === key) {
								added = true;
								dist[item].items.push(val);
								break;
							}
						}
						
						//make sure it was added
						if (!added) { dist.push({ key:key, items:[ val ]}); }
				    }
				    
				    //return the final object
				    return dist;	    
				},
				empty:_p.util.empty,
				type:_p.util.type,
				when:_p.util.when,
				each:_p.util.each,
				format:_p.util.format,
				clone:_p.util.clone,
				all:_p.util.allValues,
				as:_p.util.as
			};
			
			//apply each of the extended functions
			for(var item in _p.extend.cmd) {
				(function(params) {
				
				    //make sure this works
				    if (!(params.type || params.name || params.method)) { return; }
				
					//source commands are not added to a query
					if (params.type.match(/source/i)) { return; }
				
					//determine the correct source
					if (params.namespace && !_query[params.namespace]) {  _query[params.namespace] = {}; }
					var target = params.namespace ? _query[params.namespace] : _query;
					
					//apply this command
					var method = (function(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25) {
					
						//set the state of the command
						_s.state.debug.log("Called command %0% '%1%()'.", [params.type, params.name]);
						var state = {
							add:function(cmd) { _s.query.str.push(cmd); },
							query:_query,
							state:_s.state,
							helper:_s.helper,
							repeat:function(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25) {
								_s.query.repeatCmd(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25);
							}
						};
						
						//depending on the type, set how this acts
						if (params.type.match(/^query$/i)) {
						
							//prepare this command for use
							var cmd = _s.query.prepCmd({
								command:target[params.name],
								count:params.count,
								name:params.name
							}, [v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25]);
							
							//prepare the command for use
							_s.query.appendCmd(function(record) {
							
								//try and get the value
								try {
									state.value = _s.state.useProperties ? eval("record."+cmd.field) : record;
								}
								catch (e) {
									_s.state.debug.log("Exception when calling '%0%()' : %1%.", [params.name, e]);
									state.value = null;
								}
								
								//prepare the command
								state.record = record;
								state.type = state.helper.type(state.value);
								state.when = function(actions) {									
									return _s.helper.when(state.value, actions);
								};
							
								//query the method
								return params.method(state, cmd.arg[0], cmd.arg[1], cmd.arg[2], cmd.arg[3], cmd.arg[4],
									cmd.arg[5], cmd.arg[6], cmd.arg[7], cmd.arg[8], cmd.arg[9], cmd.arg[10], cmd.arg[11], cmd.arg[12], cmd.arg[13], cmd.arg[14], 
									cmd.arg[15], cmd.arg[16], cmd.arg[17], cmd.arg[18], cmd.arg[19], cmd.arg[20], cmd.arg[21], cmd.arg[22], cmd.arg[23], cmd.arg[24]);
							});
							
							//return the query object
							return _query;
						
						}
						
						//if an action, do the action and return the query
						else if (params.type.match(/^action$/i)) {
							
							//execute the method and return the query
							try {
								params.method(state, v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25);
							}
							catch (e) {
								_s.state.debug.log("Exception when calling '%0%()' : %1%.", [params.name, e]);
							}
							return _query;
						}
						
						//if selecting, return whatever the method returns
						else if (params.type.match(/^selection$/i)) {
						
							//if this query wants to manually select the results
							state.results = params.manual ? [] : _s.query.select();
							state.select = _s.query.select;
							
							//execute the selection method
							return params.method(state, v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25);
						
						}
						
						//return the query
						return _query;
						
					});
					
					
				    //next, assign a regular version
				    target[params.name] = method;
				    
				    //next, query methods receive extra names
				    if (params.type.match(/^query$/i) && 
				        (_p.settings.generate == null || _p.settings.generate) && 
				        (params.generate == null || params.generate)) {
				        
				        //then assign a second "or" version
				        var altName = params.name.substr(0,1).toUpperCase() + params.name.substr(1, params.name.length - 1);
    				    
    				    //create an automatic OR version
    				    target["or"+altName] = function(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25) {
				            _s.state.operator = "||";
				            return method(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25);
				        };
    				    
				        //create an automatic AND version
				        target["and"+altName] = function(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25) {
				            _s.state.operator = "&&";
				            return method(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25);
				        };
				        
				        //create an automatic NOT version
				        target["not"+altName] = function(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25) {
				            _s.state.not = !_s.state.not;
				            return method(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25);
				        };
				        
				        //create an automatic NOT version
				        target["andNot"+altName] = function(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25) {
				            _s.state.not = !_s.state.not;
				            _s.state.operator = "&&";
				            return method(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25);
				        };
				        
				        //create an automatic NOT version
				        target["orNot"+altName] = function(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25) {
				            _s.state.not = !_s.state.not;
				            _s.state.operator = "||";
				            return method(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25);
				        };
				    
				    }
			
				})(_p.extend.cmd[item]);
				
			}
		
		};
		
	};
	
	//creates a default jLinq library
	var _defaultLibrary = function() { 
		return {
			locked:true,
			generate:true,
			types:{
				array:function(val) {
					return (val.push && val.pop && val.reverse && val.slice && val.splice);
				},
				"function":function(val) {
					return ((typeof(val)).toString().match(/^function$/i));
				},
				string:function(val) {
					return ((typeof(val)).toString().match(/^string$/i));
				},
				number:function(val) {
					return ((typeof(val)).toString().match(/^number$/i));
				},
				bool:function(val) {
					return ((typeof(val)).toString().match(/^boolean$/i));
				},
				regexp:function(val) {
					return (val.ignoreCase != null && val.global != null && val.exec);
				},
				date:function(val) {
					return (val.getTime && val.setTime && val.toDateString && val.toTimeString);
				}
			},
			extend:[
			
				//Selection Methods
				//============================================================
				
				//default selection routine - selects from an array
				{name:"from", type:"source",
					method:function(query, source) {
						return query.helper.when(source, {
							"function":function() { return source(); },
							array:function() { return source; },
							other:function() { return [ source ]; }
						});
					}},
					
				//Action Methods
				//============================================================
		
				//enters into debug mode - options available
				{name:"debug", type:"action", operators:false,
					method:function(query, delegate) {
						query.state.debug.onEvent = delegate;
					}},
					
				//makes comparisons ignore case
				{name:"reverse", type:"action",
					method:function(query) {
						query.state.data.reverse();
					}},
		
				//makes comparisons ignore case
				{name:"ignoreCase", type:"action",
					method:function(query) {
						query.state.ignoreCase = true;
					}},
					
				//makes string comparisons case sensitive
				{name:"useCase", type:"action",
					method:function(query) {
						query.state.ignoreCase = false;	
					}},
					
				//flags the query for || - can be used to repeat a command
				{name:"or", type:"action",
					method:function(query, v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25) {
						query.state.operator = "||";
						query.repeat(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25);	
					}},
					
				//flags the query for ! - can be used to repeat a command	
				{name:"not", type:"action",
					method:function(query, v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25) {
						query.state.not = !query.state.not;
						query.repeat(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25);
					}},
				
				//flags the query for && - can be used to repeat a command
				{name:"and", type:"action",
					method:function(query, v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25) {
						query.state.operator = "&&";
						query.repeat(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25);
					}},
					
				//flags the query for || and ! - can be used to repeat a command
				{name:"orNot", type:"action",
					method:function(query, v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25) {
						query.state.or = true;
						query.state.not = !query.state.not;
						query.repeat(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25);	
					}},
					
				//flags the query for && and ! - can be used to repeat a command
				{name:"andNot", type:"action",
					method:function(query, v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25) {
						query.state.or = false;
						query.state.not = !query.state.not;
						query.repeat(v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25);	
					}},
					
				//combines all query selections into an expression with &&	
				{name:"combine", type:"action",
					method:function(query, delegate) {
						query.add(query.state.operator+"("+(query.state.not?"!":""));
						query.state.operator = "";
						delegate(query.query);
						query.add(")");
					}},
				
				//combines all query selections into an expression with ||
				{name:"orCombine", type:"action",
					method:function(query, delegate) {
						query.state.operator = "||";
						query.add(query.state.operator+"("+(query.state.not?"!":""));
						query.state.operator = "";
						delegate(query.query);
						query.add(")");
					}},
				
				
				//Query Methods
				//=============================================================
				
				//runs a delegate as a query, this method is definately cheating
				//since -1 is telling it not to expect a field...
				{name:"where", count:-1, type:"query",
					method:function(query, delegate) {
						return delegate(query.record, query.helper);
					}},
					
				//checks if two fields are equal or not
				{name:"has", count:1, type:"query",
					 method:function(query, value) {
						 var use = parseInt(value.toString(), 16);
						 var compare = parseInt(query.value.toString(), 16);
						 return ((compare & use) == use);
					}},
					
				//checks if two fields are equal or not
				{name:"equals", count:1, type:"query",
					method:function(query, value) {
						return query.helper.equals(query.value, value);				
					}},
					
				//returns if a string starts with the phrase
				//returns if the first element in array is equal
				{name:"startsWith", count:1, type:"query",
					method:function(query, value) {
					
						//allow arrays to be passed as comparisons
						if (query.helper.type(value) != "array") {
							value = [value];
						}
						
						//check for each value
						for (var item in value) {
							var match = value[item];
							if  (query.when({
								array:function() {
									return query.helper.equals(query.value[0], match)
								},
								other:function() {
									return query.helper.match(query.value.toString(), "^"+match.toString());
								}
							})) { return true; };	
						}
					}},
					
				//returns if a string ends with a phrase
				//returns if the last element in array is equal
				{name:"endsWith", count:1, type:"query",
					method:function(query, value) {
						
						//allow arrays to be passed as comparisons
						if (query.helper.type(value) != "array") {
							value = [value];
						}
						
						//check for each value
						for (var item in value) {
							var match = value[item];
							if  (query.when({
								array:function() {
									return query.helper.equals(query.value[query.value.length - 1], match)
								},
								other:function() {
									return query.helper.match(query.value.toString(), match.toString()+"$");
								}
							})) { return true; };	
						}				
					}},
					
				//returns if a string contains a phrase
				//returns if any element in array is equal
				{name:"contains", count:1, type:"query",
					method:function(query, value) {
						if (value == null) { return false; }
						
						//allow arrays to be passed as comparisons
						if (query.helper.type(value) != "array") {
							value = [value];
						}
						
						//check for each value
						for (var item in value) {
							var match = value[item];
							if  (query.when({
								array:function() {
									for (var i = 0; i < query.value.length; i++) {
										if (query.helper.equals(query.value[i], match)) { return true; }
									}
								},
								other:function() {
									return query.helper.match(query.value.toString(), "^.*" + query.helper.toRegex(match) + ".*$");
								}
							})) { return true; };	
						}
					}},
					
				//evaluates each item with a regular expression
				{name:"match", count:1, type:"query",
					method:function(query, value) {

						//allow arrays to be passed as comparisons
						if (query.helper.type(value) != "array") {
							value = [value];
						}
						//TODO : Convert items into regular expressions
						//array:(item1|item2|item3|item4)
						
						//check for each value
						for (var item in value) {
							var match = value[item];
							if  (query.when({
								array:function() {
									for (var i = 0; i < query.value.length; i++) {
										if (query.helper.match(query.value[i], match)) { return true; }
									}
								},
								other:function() {
									return query.helper.match(query.value.toString(), match);
								}
							})) { return true; };	
						}
						
					}},
					
				//returns if a number is less
				//returns if a string has less characters
				//returns if an array has less elements
				{name:"less", count:1, type:"query",
					method:function(query, value) {
					
					    //get the value to use with this
					    value = query.helper.when(value, {
					        number:function() { return value; },
					        date:function() { return value; },
					        other:function() { return value.length; }
					    });
					    
						return query.when({
							string:function() {
								return (query.value.length < value);
							},
							array:function() {
								return (query.value.length < value);
							},
							other:function() {
								return (query.value < value);
							}
						});									
					}},
				
				//returns if a number is more
				//returns if a string has more characters
				//returns if an array has more elements
				{name:"greater", count:1, type:"query",
					method:function(query, value) {
						
					    //get the value to use with this
					    value = query.helper.when(value, {
					        number:function() { return value; },
					        date:function() { return value; },
					        other:function() { return value.length; }
					    });
					    
						return query.when({
							string:function() {
								return (query.value.length > value);
							},
							array:function() {
								return (query.value.length > value);
							},
							other:function() {
								return (query.value > value);
							}
						});					
					}},
				
				//returns if a number is less
				//returns if a string has less characters
				//returns if an array has less elements
				{name:"lessEquals", count:1, type:"query",
					method:function(query, value) {
						
					    //get the value to use with this
					    value = query.helper.when(value, {
					        number:function() { return value; },
					        date:function() { return value; },
					        other:function() { return value.length; }
					    });
					    
						return query.when({
							string:function() {
								return (query.value.length <= value);
							},
							array:function() {
								return (query.value.length <= value);
							},
							other:function() {
								return (query.value <= value);
							}
						});									
					}},
				
				//returns if a number is more
				//returns if a string has more characters
				//returns if an array has more elements
				{name:"greaterEquals", count:1, type:"query",
					method:function(query, value) {
						
						//get the value to use with this
					    value = query.helper.when(value, {
					        number:function() { return value; },
					        date:function() { return value; },
					        other:function() { return value.length; }
					    });
					    
						return query.when({
							string:function() {
								return (query.value.length >= value);
							},
							array:function() {
								return (query.value.length >= value);
							},
							other:function() {
								return (query.value >= value);
							}
						});			
					}},
				
				//returns if a number is more
				//returns if a string has more characters
				//returns if an array has more elements
				{name:"between", count:2, type:"query",
					method:function(query, low, high) {
						
					    //get the value to use with this
					    low = query.helper.when(low, {
					        number:function() { return low; },
					        date:function() { return value; },
					        other:function() { return low.length; }
					    });
					    
					    high = query.helper.when(high, {
					        number:function() { return high; },
					        other:function() { return high.length; }
					    });
					    
						return query.when({
							string:function() {
								return (query.value.length > low && query.value.length < high);
							},
							array:function() {
								return (query.value.length > low && query.value.length < high);
							},
							other:function() {
								return (query.value > low && query.value < high);
							}
						});				
					}},
					
				//returns if a number is more
				//returns if a string has more characters
				//returns if an array has more elements
				{name:"betweenEquals", count:2, type:"query",
					method:function(query, low, high) {
						low = query.helper.when(low, {
					        number:function() { return low; },
					        date:function() { return value; },
					        other:function() { return low.length; }
					    });
					    
					    high = query.helper.when(high, {
					        number:function() { return high; },
					        date:function() { return value; },
					        other:function() { return high.length; }
					    });
					    
						return query.when({
							string:function() {
								return (query.value.length >= low && query.value.length <= high);
							},
							array:function() {
								return (query.value.length >= low && query.value.length <= high);
							},
							other:function() {
								return (query.value >= low && query.value <= high);
							}
						});					
					}},
				
				//returns if a field is null
				//returns if an array is empty
				//returns if a string is empty
				{name:"empty", count:0, type:"query",
					method:function(query) {
						return query.when({
							array:function() {
								return (query.value.length == 0);
							},
							string:function() {
								return (query.value == "");
							},
							empty:function() {
								return true;
							}
						});
					}},
				
				//returns if a boolean iss true
				//returns if a record has a field
				{name:"is", count:0, type:"query",
					method:function(query) {
						return query.when({
							bool:function() {
								return query.value;
							},
							empty:function() {
								return false;
							},
							other:function() {
								return (query.value != null);
							}
						});
					}},
				
				//returns if a boolean is false
				//returns if a record is missing a field
				{name:"isNot", count:0, type:"query",
					method:function(query) {
						return query.when({
							bool:function() {
								return !query.value;
							},
							empty:function() {
								return true;
							},
							other:function() {
								return (query.value == null);
							}
						});
					}},
				
				
				//Selection Methods 
				//=============================================================
				
				//returns if any records match the query
				{name:"any", type:"selection",
					method:function(query) {
						return (query.results.selected.length > 0);
					}},
				
				//returns if all records match the query
				{name:"all", type:"selection",
					method:function(query) {
						return (query.results.selected.length == query.state.data.length);
					}},
					
				//returns none of the records match
				{name:"none", type:"selection",
					method:function(query) {
						return !query.query.all();
					}},
				
				//returns the total records found
				{name:"count", type:"selection",
					method:function(query, invert) {
						return invert ? query.results.remaining.length : query.results.selected.length;
					}},
				
				//returns array of matching records
				{name:"select", type:"selection",
					method:function(query, selection, invert) {
						
						//select the records
						var records = [];
						var results = invert ? query.results.remaining : query.results.selected;
						selection = query.helper.type(selection) == "function" ? selection : function(r) { return r; };
						for (var i = 0; i < results.length; i++) {
							records.push(selection(results[i]));
						}
						
						//return the final records
						return records;
						
					}},
					
				//returns HTML string for a table
				{name:"toTable", type:"selection", manual:true,
					method:function(query, params, selection, invert) {
						
						params = params ? params : {};
						var results = query.query.select(selection, invert);
						
						//create the table structure
						if (results.count == 0) { return "No results for this query"; }
						
						//getting a string
						var getString = function(raw) {							
							query.helper.when(raw, {
								date:function() {
									raw = query.helper.format("%0%/%1%/%2% at %3%:%4% %5%", 
										[raw.getMonth()+1, raw.getDate(), raw.getFullYear(), (raw.getHours() > 12 ? raw.getHours() - 12 : raw.getHours()),  raw.getMinutes(), (raw.getHours() > 12 ? "PM" : "AM")]
										);
								},
								empty:function() {
									raw = "null";
								},
								other:function() {
									raw = raw.toString();
								}
							});
							return raw;
						};
						
						//otherwise, start the table
						var output = ["<table cellpadding='0' cellspacing='0' " + 
						    (params.border?"border='" + params.border + "' ":"") + 
						    (params.css?"class='" + params.css + "' ":"") + " >"];
						
						//create the header
						if (query.state.useProperties) {
							var columns = [];
							output.push("<tr>");
							for(var item in results[0]) {
								columns.push(item);
								output.push("<th>");
								output.push(escape(item));
								output.push("</th>");
							}
							output.push("</tr>");
						}
						
						//do each item
						var alt = true;
						for (var i = 0; i < results.length; i++) {
						    alt = !alt;
							var record = results[i];							
							output.push("<tr " + (alt?"class='alt-row'":"") + ">");
							
							//create the row
							if (query.state.useProperties) {
								for(var col in columns) {
								
									//get a formatted string
									var item = columns[col];
									var val = record[item];
									var msg = getString(val);									
								
									//add the information
									output.push("<td>");
									output.push(msg);
									output.push("</td>");
								}
							}
							else {
								//no properties
								output.push("<td>");
								output.push(getString(record));
								output.push("</td>");
							}
							
							//close the row
							output.push("</tr>");
						
						}
						
						//close the table and return the output
						output.push("</table>");
						return output.join("");			
					
					}},
				
				//Executes the action to each match then returns the query - technically a selection
				{name:"each", type:"selection", manual:true,
					method:function(query, action, selection, invert) {
					
						//select the correct records and perform the action
						var results = query.query.select(selection, invert);
						for(var i = 0; i < results.length; i++) {
							action(results[i], i);
						}				
						
						//return the query again
						return query.query;
						
					}},
					
				//Orders the records then returns the query - technically a selection
				{name:"orderBy", type:"selection", manual:true,
					method:function(query, v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25) {
					    var order = query.helper.all([v1,v2,v3,v4,v5,v6,v7,v8,v9,v10,v11,v12,v13,v14,v15,v16,v17,v18,v19,v20,v21,v22,v23,v24,v25]);
                        if (order.length == 0) { order = [""]; }
                        
						//sort depending on if there are properties or not
						if (!query.state.useProperties) {						
						    var desc = (order.length > 0) ? (order[0]+"").match(/^\-/g) : false;							
						    query.state.data = query.helper.sort(query.state.data, null, desc);	
							return query.query;
						}

                        //since this is more complicated, build the sorting order
                        var sorting = [];
						for (var i = 0; i < order.length; i++) {
						    sorting.push({ 
						        desc: (order[i].substr(0,1) == "-"),
						        field: order[i].replace(/^\-/g, "")
						        });
						}
						query.state.data = query.helper.sort(query.state.data, sorting);						
						
						//return a new query
						return query.query;				
					
					}},
			
				//returns all distinct values based on the field
				{name:"distinct", type:"selection",
					method:function(query, field, invert) {						
						var sel = invert ? query.results.remaining : query.results.selected;
						var results = query.helper.distinct(sel, field);
						
						//return just the names of the fields
						var dist = [];
						for (var item in results) {
						    dist.push(results[item].key);
						}
						
						//sort it to be helpful
						return query.helper.sort(dist, null, false);
					
					}},
				
				//groups the records and returns 
				{name:"groupBy", type:"selection",
					method:function(query, field, invert) {	
						var sel = invert ? query.results.remaining : query.results.selected;
						var results = query.helper.distinct(sel, field);
						return jLinq.from(results);					
					}},	
			
				//Joins a second array to the current query
				{name:"join", type:"selection", 
					method:function(query, source, alias, pk, fk) {

                        //clone the source array
                        source = query.helper.clone(source);
												
						//create a second query for this item
						var gen = [];
						for (var i = 0; i < query.state.data.length; i++) {
							var record = query.helper.clone(query.state.data[i]);
							var results = jLinq.from(source).equals(fk, record[pk]).select();
							if (results.length == 1) { 
								record[alias] = results[0];
							}
							else {
								record[i][alias] = results;
							}
							
							//add the new record
							gen.push(record);
						}
						
						//return a new query
						return jLinq.from(gen);
					
					}},

				//attaches a property to each record
				{name:"attach", type:"selection", 
					method:function(query, alias, delegate) {					
					    for(var i = 0; i < query.state.data.length; i++) {
					        query.state.data[i][alias] = delegate(query.state.data[i]);
					    }					
					}},
			
				//skips and takes the correct set of records
				{name:"skipTake", type:"selection", manual:true,
					method:function(query, skip, take, selection, invert) {
						skip = Math.max(query.helper.type(skip) == "number" ? skip : 0, 0);
						take = Math.max(query.helper.type(take) == "number" ? take : 0, 0);
						
						//take the correct number of records
						var results = query.query.select(selection, invert);
						return results.slice(skip, (skip + take));
						
					}},
					
				//skips and takes the correct set of records
				{name:"take", type:"selection", manual:true,
					method:function(query, take, selection, invert) {
						take = Math.max(query.helper.type(take) == "number" ? take : 0, 0);
						
						//take the correct number of records
						var results = query.query.select(selection, invert);
						return results.slice(0, take );
						
					}},
			
				//the first match of the set -- allows a default match if nothing is found
				{name:"first", type:"selection", manual:true,
					method:function(query, defType, selection, invert) {
						var results = query.query.select(selection, invert);
						return results.length > 0 ? results[0] : defType ? defType : null;
					}},
				
				//the last match of the set -- allows a default match if nothing is found
				{name:"last", type:"selection", manual:true,
					method:function(query, defType, selection, invert) {
						var results = query.query.select(selection, invert);
						return results.length > 0 ? results[results.length - 1] : defType ? defType : null;
					}},
				
				//the element at the specified index -- allows a default match if nothing is found
				{name:"at", type:"selection", manual:true,
					method:function(query, index, defType, selection, invert) {
						var results = query.query.select(selection, invert);
						return index < results.length || index >= 0 ? results[index] : defType ? defType : null;
					}},
					
				//returns the sum of all fields of a type	
				{name:"sum", type:"selection",
					method:function(query, field, invert) {						
						if (!query.state.useProperties) { invert = field; }
						var sel = invert ? query.results.remaining : query.results.selected;
						var result = 0;
						query.helper.each(sel, function(rec) {
							if (query.state.useProperties) {
								if (result == null) { 
									result = rec[field]; 
								} 
								else { 
									query.helper.when(rec[field], {
										array:function() {
											result += rec[field].length;
										},
										string:function() {
											result += rec[field].length;
										},
										other:function() {
											result += rec[field]; 
										}});
								}
							}
							else {
								query.helper.when(rec, {
									array:function() {
										result += rec.length;
									},
									string:function() {
										result += rec.length;
									},
									other:function() {
										result += rec; 
									}});
							}
						});
						return {
							count:sel.length,
							result:result,
							records:sel
						};
					}},
				
				//returns the average of a sum selection
				{name:"average", type:"selection",
					method:function(query, field, invert) {	
						var sel = invert ? query.results.remaining : query.results.selected;
						var sum = jLinq.from(sel).sum(field).result;
						return {
							total:sum,
							count:sel.length,
							result:(sum / sel.length),
							records:sel
						};
					}},
					
				//returns the maximum value
				{name:"max", type:"selection",
					method:function(query, field, invert) {	
						var list = jLinq.from(invert ? query.results.remaining : query.results.selected)
							.select(function(r) {
								r = query.state.useProperties ? r[field] : r;
								return {
									value:r,
									count:query.helper.getNumericValue(r)
								}
							});
						return jLinq.from(list).orderBy("count", "value").last()["value"];
					}},
					
				//returns the minimum value
				{name:"min", type:"selection",
					method:function(query, field, invert) {	
						var list = jLinq.from(invert ? query.results.remaining : query.results.selected)
							.select(function(r) {
								r = query.state.useProperties ? r[field] : r;
								return {
									value:r,
									count:query.helper.getNumericValue(r)
								}
							});
						return jLinq.from(list).orderBy("count", "value").first()["value"];
					}},
				
				//contains any values not found in comparison each array
				{name:"except", type:"selection",
					method:function(query, compare, invert) {
						var selection = invert ? query.results.remaining : query.results.selected;
						if (!(compare && compare.length && compare.length > 0)) { return jLinq.from(selection); }
						
						//return only results that don't match
						var result = jLinq.from(selection)
							.notWhere(function(val) {
								for (var i = 0; i < compare.length; i++) {
									if (query.state.useProperties) {
										if (query.helper.propsEqual(compare[i], val)) { return true; }
									}
									else {
										if (query.helper.equals(compare[i], val)) { return true; }
									}
								}
								return false;
							})
							.select();							
						return jLinq.from(result);
					}},
					
				//returns unique values from two arrays
				{name:"intersect", type:"selection",
					method:function(query, compare, invert) {
						var selection = invert ? query.results.remaining : query.results.selected;
						if (!(compare && compare.length && compare.length > 0)) { return jLinq.from(selection); }
						
						//return only results that don't match
						var result = jLinq.from(selection)
							.where(function(val) {
								for (var i = 0; i < compare.length; i++) {
									if (query.state.useProperties) {
										if (query.helper.propsEqual(compare[i], val)) { return true; }
									}
									else {
										if (query.helper.equals(compare[i], val)) { return true; }
									}
								}
								return false;
							})
							.select();							
						return jLinq.from(result);
					}},
					
				//merges two arrays together
				{name:"union", type:"selection",
					method:function(query, compare, invert) {
						var selection = invert ? query.results.remaining : query.results.selected;
						if (!(compare && compare.length && compare.length > 0)) { return jLinq.from(selection); }
						
						//return the results to make a single list
						return jLinq.from(compare.concat(
							jLinq.from(selection)
							.where(function(val) {
								for (var i = 0; i < compare.length; i++) {
									if (query.state.useProperties) {
										if (query.helper.propsEqual(compare[i], val)) { return false; }
									}
									else {
										if (query.helper.equals(compare[i], val)) { return false; }
									}
								}
								return true;
							})
							.select()
							));
					}},
					
				//skips records until the first condition is met
				{name:"skipWhile", type:"selection",
					method:function(query, delegate, invert) {
						var selection = invert ? query.results.remaining : query.results.selected;
						
						//return the results to make a single list
						var skip = true;
						return jLinq.from(selection)
							.where(function(rec, helper) {
								if (skip) { skip = delegate(rec, helper); }
								return !skip;
							})
							.select();
					}},
					
				//takes records till the first condition is met
				{name:"takeWhile", type:"selection",
					method:function(query, delegate, invert) {
						var selection = invert ? query.results.remaining : query.results.selected;
						
						//return the results to make a single list
						var take = true;
						return jLinq.from(selection)
							.where(function(rec, helper) {
								if (take) { take = delegate(rec, query.helper); }
								return take;
							})
							.select();
					}},

				//merges two arrays together
				{name:"selectMany", type:"selection",
					method:function(query, collection, delegate, select, invert) {
						var selection = invert ? query.results.remaining : query.results.selected;
						
						//return the results to make a single list
						select = select ? select : function(r,s) { return { source:r, compare:s }; };
						var results = [];
						query.helper.each(selection, function(rec) {
							query.helper.each(collection, function(sub) {
								if (delegate(rec, sub)) { results.push(select(rec,sub)); }
							});
						});
						return results;
					}}
			
			]
			
		}; 
	
	}; //default library
	
	//create the base library
	jLinq = new library(_defaultLibrary());
	jLinq.finish(true);
	
	//Create entirely new libraries
	jLinq.library = function(settings, imp) {
		if (imp == null) { imp = true; }
		
		//if no defaults, return it as is
		var lib = new library(_defaultLibrary());
		
		//clear out the info if not importing
		if (!imp) { 
			lib.types = {};
			lib.extend = [];
		}
		
		//import any settings, if any
		var lock = false;
		if (settings) {
		
			//extend the methods
			if (settings.extend) {
				for (var ext in settings.extend) {
					lib.extend(settings.extend[ext])
				}
			}
			
			//extend the types
			if (settings.types) {
				for (var type in settings.types) {
					lib.addType(settings.types[type]);;
				}
			}

			//if there is a lock setting			
			if (settings.locked) { lock = settings.locked; }
		
		};
		
		//set the lock and return
		lib.finish(lock);
		return lib;
		
	};
	
})();