using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.WebHost.Endpoints.Endpoints
{
    public class ServiceOperations
    {
        const string RESPONSE_SUFFIX = "Response";

        public ServiceOperations(Type serviceOperationType, IEnumerable<string> replyOperationVerbs, IEnumerable<string> oneWayOperationVerbs)
        {
            var dtoTypes = serviceOperationType.Assembly.GetTypes();
            ReplyOperations = GetOperationTypes(serviceOperationType, replyOperationVerbs, dtoTypes);
            OneWayOperations = GetOperationTypes(serviceOperationType, oneWayOperationVerbs, dtoTypes);
            AllOperations = Merge(ReplyOperations, OneWayOperations);
        }

        public Operations ReplyOperations { get; private set; }
        public Operations OneWayOperations { get; private set; }
        public Operations AllOperations { get; private set; }

        private static Operations Merge(params Operations[] operations)
        {
            var mergedOperations = new Operations();
            foreach (var operation in operations)
            {
                mergedOperations.Names.AddRange(operation.Names);
                mergedOperations.Types.AddRange(operation.Types);
            }
            return mergedOperations;
        }

        private static Operations GetOperationTypes(Type serviceOperationType, 
            IEnumerable<string> operationVerbs, IEnumerable<Type> dtoTypes)
        {
            var operations = new Operations();

            operations.Types.AddRange(dtoTypes.Where(type =>
                       type.Namespace == serviceOperationType.Namespace
                       && operationVerbs.Any(verb => type.Name.ToLower().StartsWith(verb))));

            operations.Names.AddRange(from type in operations.Types
                                      where !type.Name.EndsWith(RESPONSE_SUFFIX)
                                      orderby type.Name
                                      select type.Name);
            return operations;
        }
    }
}