using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ServiceStack.ServiceHost
{
    public class ServiceMetadata
    {
        public ServiceMetadata()
        {
            this.RequestTypes = new HashSet<Type>();
            this.ServiceTypes = new HashSet<Type>();
            this.ResponseTypes = new HashSet<Type>();
            this.OperationsMap = new Dictionary<Type, Operation>();
            this.OperationsResponseMap = new Dictionary<Type, Operation>();
            this.OperationNamesMap = new Dictionary<string, Operation>();
        }

        public Dictionary<Type, Operation> OperationsMap { get; protected set; }
        public Dictionary<Type, Operation> OperationsResponseMap { get; protected set; }
        public Dictionary<string, Operation> OperationNamesMap { get; protected set; }
        public HashSet<Type> RequestTypes { get; protected set; }
        public HashSet<Type> ServiceTypes { get; protected set; }
        public HashSet<Type> ResponseTypes { get; protected set; }
        
        public IEnumerable<Operation> Operations
        {
            get { return OperationsMap.Values; }
        }

        public void Add(Type serviceType, Type requestType, Type responseType)
        {
            this.ServiceTypes.Add(serviceType);
            this.RequestTypes.Add(requestType);

            var restrictTo = requestType.GetCustomAttributes(true)
                    .OfType<RestrictAttribute>().FirstOrDefault() 
                ?? serviceType.GetCustomAttributes(true)
                    .OfType<RestrictAttribute>().FirstOrDefault();

            var operation = new Operation {
                ServiceType = serviceType,
                RequestType = requestType, 
                ResponseType = responseType,
                RestrictTo = restrictTo,
                Actions = GetImplementedActions(serviceType, requestType),
            };

            this.OperationsMap[requestType] = operation;
            this.OperationNamesMap[requestType.Name.ToLower()] = operation;

            if (responseType != null)
            {
                this.ResponseTypes.Add(responseType);
                this.OperationsResponseMap[responseType] = operation;
            }
        }

        public List<string> GetImplementedActions(Type serviceType, Type requestType)
        {
            if (typeof(IService).IsAssignableFrom(serviceType))
            {
                return serviceType.GetActions().Select(x => x.Name.ToUpper()).ToList();
            }

            var oldApiActions = serviceType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(x => ToNewApiAction(x.Name))
                .Where(x => x != null)
                .ToList();
            return oldApiActions;
        }

        public static string ToNewApiAction(string oldApiAction)
        {
            switch (oldApiAction)
            {
                case "Get":
                case "OnGet":
                    return "GET";
                case "Put":
                case "OnPut":
                    return "PUT";
                case "Post":
                case "OnPost":
                    return "POST";
                case "Delete":
                case "OnDelete":
                    return "DELETE";
                case "Patch":
                case "OnPatch":
                    return "PATCH";
                case "Execute":
                case "Run":
                    return "ANY";
            }
            return null;
        }

        public Type GetOperationType(string operationTypeName)
        {
            Operation operation;
            OperationNamesMap.TryGetValue(operationTypeName.ToLower(), out operation);
            return operation != null ? operation.RequestType : null;
        }

        public Type GetServiceTypeByRequest(Type requestType)
        {
            Operation operation;
            OperationsMap.TryGetValue(requestType, out operation);
            return operation != null ? operation.ServiceType : null;
        }

        public Type GetServiceTypeByResponse(Type responseType)
        {
            Operation operation;
            OperationsResponseMap.TryGetValue(responseType, out operation);
            return operation != null ? operation.ServiceType : null;
        }

        public Type GetResponseTypeByRequest(Type requestType)
        {
            Operation operation;
            OperationsMap.TryGetValue(requestType, out operation);
            return operation != null ? operation.ResponseType : null;
        }

        public List<Type> GetAllTypes()
        {
            var allTypes = new List<Type>(RequestTypes);
            allTypes.AddRange(ResponseTypes);
            return allTypes;
        }

        public List<string> GetAllOperationNames()
        {
            return Operations.Select(x => x.RequestType.Name).ToList();
        }
    }

    public class Operation
    {
        public Type RequestType { get; set; }
        public Type ServiceType { get; set; }
        public Type ResponseType { get; set; }
        public RestrictAttribute RestrictTo { get; set; }
        public List<string> Actions { get; set; }
        public bool IsOneWay { get { return ResponseType == null; } }
    }

    public class XsdMetadata
    {
        public ServiceMetadata Metadata { get; set; }
        public bool Flash { get; set; }
        public bool IncludeAllTypes { get; set; }

        public XsdMetadata(ServiceMetadata metadata, bool flash = false, bool includeAllTypes = true)
        {
            Metadata = metadata;
            Flash = flash;
            IncludeAllTypes = includeAllTypes;
        }

        public List<Type> GetAllTypes()
        {
            var allTypes = new List<Type>(Metadata.RequestTypes);
            if (IncludeAllTypes)
                allTypes.AddRange(Metadata.ResponseTypes);
            return allTypes;
        }

        public List<string> GetReplyOperationNames()
        {
            return Metadata.OperationsMap.Values
                .Where(x => !x.IsOneWay)
                .Select(x => x.RequestType.Name)
                .ToList();
        }

        public List<string> GetOneWayOperationNames()
        {
            return Metadata.OperationsMap.Values
                .Where(x => x.IsOneWay)
                .Select(x => x.RequestType.Name)
                .ToList();
        }

        /// <summary>
        /// Gets the name of the base most type in the heirachy tree with the same.
        /// 
        /// We get an exception when trying to create a schema with multiple types of the same name
        /// like when inheriting from a DataContract with the same name.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static Type GetBaseTypeWithTheSameName(Type type)
        {
            var typesWithSameName = new Stack<Type>();
            var baseType = type;
            do
            {
                if (baseType.Name == type.Name)
                    typesWithSameName.Push(baseType);
            }
            while ((baseType = baseType.BaseType) != null);

            return typesWithSameName.Pop();
        }
    }

}