function RedisClient(baseUri) {
    RedisClient.$baseConstructor.call(this);

    this.gateway = new JsonServiceClient(baseUri);
}
RedisClient.errorFn = function() {
};
RedisClient.extend(AjaxStack.ASObject, { type: "AjaxStack.RedisClient" },
{
    echo: function(text, onSuccessFn, onErrorFn) 
    {
        this.gateway.getFromJsonService("Echo", { Text: text }, 
            function(r) 
            {
                if (onSuccessFn) onSuccessFn(r.getResult().Text);
            },
            onErrorFn || RedisClient.errorFn);
    },
    ping: function(onSuccessFn, onErrorFn) {
        this.gateway.getFromJsonService("Ping", {},
            function(r) 
            {
                if (onSuccessFn) onSuccessFn(r.getResult().Result);
            },
            onErrorFn || RedisClient.errorFn);
    },
    appendToValue: function(key, value, onSuccessFn, onErrorFn) {
        this.gateway.getFromJsonService("AppendToValue", { Key: key, Value: value }, function(r) {
            if (onSuccessFn) onSuccessFn(r.getResult().ValueLength);
        },
        onErrorFn || RedisClient.errorFn);
    },
    containsKey: function(key, value, onSuccessFn, onErrorFn) {
        this.gateway.getFromJsonService("ContainsKey", { Key: key, Value: value }, function(r) {
            if (onSuccessFn) onSuccessFn(r.getResult().ValueLength);
        },
        onErrorFn || RedisClient.errorFn);
    },
    setEntryIfNotExists: function(key, value, onSuccessFn, onErrorFn) {
        this.gateway.getFromJsonService("SetEntryIfNotExists", { Key: key, Value: value }, function(r) {
            if (onSuccessFn) onSuccessFn(r.getResult().Result);
        },
        onErrorFn || RedisClient.errorFn);
    }
});