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
        this.gateway.getFromService("Echo", { Text: text }, 
            function(r) 
            {
                if (onSuccessFn) onSuccessFn(r.getResult().Text);
            },
            onErrorFn || RedisClient.errorFn);
    },
    ping: function(onSuccessFn, onErrorFn) {
        this.gateway.getFromService("Ping", {},
            function(r) 
            {
                if (onSuccessFn) onSuccessFn(r.getResult().Result);
            },
            onErrorFn || RedisClient.errorFn);
    },
    appendToValue: function(key, value, onSuccessFn, onErrorFn) {
        this.gateway.getFromService("AppendToValue", { Key: key, Value: value }, function(r) {
            if (onSuccessFn) onSuccessFn(r.getResult().ValueLength);
        },
        onErrorFn || RedisClient.errorFn);
    },
    containsKey: function(key, value, onSuccessFn, onErrorFn) {
        this.gateway.getFromService("ContainsKey", { Key: key, Value: value }, function(r) {
            if (onSuccessFn) onSuccessFn(r.getResult().ValueLength);
        },
        onErrorFn || RedisClient.errorFn);
    },
    setEntryIfNotExists: function(key, value, onSuccessFn, onErrorFn) {
        this.gateway.getFromService("SetEntryIfNotExists", { Key: key, Value: value }, function(r) {
            if (onSuccessFn) onSuccessFn(r.getResult().Result);
        },
        onErrorFn || RedisClient.errorFn);
    }
});