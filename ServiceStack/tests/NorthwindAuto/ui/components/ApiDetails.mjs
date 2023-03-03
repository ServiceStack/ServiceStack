import { computed, inject, nextTick, onMounted, onUnmounted, ref, watch } from "vue"
import { prettyJson } from "core"
import { ApiResult, HttpMethods, parseCookie, map, humanify, humanize } from "@servicestack/client"
import { useMetadata, useClient, useUtils, css } from "@servicestack/vue"
import { hasItems } from "core";

const { createDto } = useMetadata()

export const ApiDetails = {
    template:/*html*/`
    <div class="w-body md:w-body fixed top-top-nav h-top-nav flex">
        <div v-if="request && !(routes.detailSrc && !breakpoints.lg)" id="detail-info" class="flex-1 w-full lg:w-1/2 pt-2 sm:px-4 bg-white overflow-auto md:pb-scroll">
          <div class="flex px-2 sm:px-0">
            <div class="flex-grow">
              <h2 class="mt-1 mb-3 flex items-center">
                <span v-if="op.method" title="HTTP Method"
                      class="mr-2 inline-flex items-center text-md px-2.5 py-0.5 bg-gray-100 text-gray-600">
                  {{op.method}}
                </span>
                <a v-href="{ detailSrc: opName }" class="text-xl text-blue-800">
                  {{opName}}
                </a>
              </h2>
        
              <div class="mb-3 pl-4">
                <div v-for="route in op.routes" class="text-sm text-gray-900">
                    <span v-for="verb in (route.verbs||'').split(',').filter(verb => verb && verb != op.method)"
                          class="font-medium mr-1">{{verb}}</span>
                  {{route.path}}
                </div>
                <div v-if="predefinedRoute(request.name)" class="text-sm text-gray-900">
                  {{predefinedRoute(request.name)}}
                </div>
              </div>
        
              <div v-if="op.tags.length > 0" class="mb-3">
                <span v-for="tag in op.tags" title="tag"
                      class="inline-flex items-center px-2.5 py-0.5 mr-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                  {{tag}}
                </span>
              </div>
            </div>
            <div v-if="op.requiresAuth">
              <div class="flex mb-3">
                <span class="flex-grow text-gray-500 mr-2">Authentication <span class="hidden lg:inline lg:ml-1">Required</span></span>
                <span class="svg-lock w-5 h-5"></span>
              </div>
              <div v-if="hasItems(op.requiredRoles)" class="mb-3 flex flex-wrap justify-end">
                <span v-for="role in op.requiredRoles" title="required role"
                      class="inline-flex items-center px-2.5 py-0.5 mr-1 mb-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
                  {{role}}
                </span>
              </div>
              <div v-if="hasItems(op.requiredPermissions)" class="mb-3 flex flex-wrap justify-end">
                <span v-for="perm in op.requiredPermissions" title="required permission"
                      class="inline-flex items-center px-2.5 py-0.5 mr-1 mb-1 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
                  {{perm}}
                </span>
              </div>
              <div v-if="hasItems(op.requiresAnyRole)" class="mb-3 flex flex-wrap justify-end">
                <span class="text-gray-400 mr-2">any role</span>
                <span v-for="role in op.requiresAnyRole" title="role"
                      class="inline-flex items-center px-2.5 py-0.5 mr-1 mb-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
                  {{role}}
                </span>
              </div>
              <div v-if="hasItems(op.requiresAnyPermission)" class="mb-3 flex flex-wrap justify-end">
                <span class="text-gray-400 mr-2">any permission</span>
                <span v-for="perm in op.requiresAnyPermission" title="permission"
                      class="inline-flex items-center px-2.5 py-0.5 mr-1 mb-1 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800">
                  {{perm}}
                </span>
              </div>
            </div>
          </div>
          <div class="px-2 sm:px-0">
            <div class="flex justify-between w-full">
              <div>
                <div v-if="request.inherits" class="mb-3">
                  <span class="text-gray-500 mr-2">inherits</span>
                  <a v-href="{ detailSrc: inheritedTypes(request.inherits) }" class="text-blue-800">
                    {{typeName(request.inherits)}}
                  </a>
                </div>
                <div v-if="request.implements.length" class="mb-3">
                  <span class="text-gray-500 mr-2">implements</span>
                  <span class="text-gray-900">
                        {{request.implements.map(iface => typeName(iface)).join(', ')}}
                    </span>
                </div>
              </div>
              <div>
                <div v-if="op.returnType || op.returnsVoid">
                  <span class="text-gray-500 mr-2">returns</span>
                  <a v-if="op.returnType" v-href="{ detailSrc: op.returnType.name }" class="text-blue-800">
                    {{typeName(op.returnType)}}
                  </a>
                  <span v-else class="text-blue-800">void</span>
                </div>
              </div>
            </div>
          </div>
        
          <div>
            <div v-for="group in opReferencedTypes(op)">
              <div class="flex flex-col mb-3">
                <component v-if="apiDocs(group[0].typeName)" :is="apiDocs(group[0].typeName)" />
                <div v-else-if="group[0]">
                  <h2 v-if="group[0].type.description" class="my-3 text-gray-700 text-2xl text-center">
                    {{group[0].type.description}}</h2>
                  <div v-if="group[0].type.notes"
                       class="notes mb-3 text-gray-700 bg-gray-50 p-4 text-center rounded"
                       v-html="group[0].type.notes"></div>
                  <h3 class="font-medium text-gray-500 text-center my-3">{{ group[0].typeName }}</h3>
                </div>
                <div class="">
                  <div class="py-2 align-middle inline-block min-w-full sm:px-2 lg:px-4">
                    <div v-if="group[0].type.isEnum" class="shadow overflow-hidden border-b border-gray-200 sm:rounded-lg my-3">
                      <table class="min-w-full divide-y divide-gray-200">
                        <thead class="bg-gray-50">
                        <tr>
                          <th scope="col"
                              class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                            Enum Name
                          </th>
                        </tr>
                        </thead>
                        <tbody>
                        <tr v-for="value in group[0].type.enumNames">
                          <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                            {{value}}
                          </td>
                        </tr>
                        </tbody>
                      </table>
                    </div>
                    <div v-else-if="group.some(x => x.type.properties && x.type.properties.length)" class="shadow overflow-hidden border-b border-gray-200 sm:rounded-lg my-3">
                      <table class="min-w-full divide-y divide-gray-200">
                        <thead class="bg-gray-50">
                        <tr>
                          <th scope="col"
                              class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                            Name
                          </th>
                          <th scope="col"
                              class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                            Type
                          </th>
                          <th scope="col"
                              class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                            Required
                          </th>
                          <th scope="col"
                              class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                            Description
                          </th>
                        </tr>
                        </thead>
                        <tbody v-for="(entry, index) in group">
                        <tr v-if="index > 0" :class="'bg-gray-50 border-b text-gray-600'">
                          <td colspan="4" class="text-center py-2">{{entry.typeName}}</td>
                        </tr>
                        <tr v-for="(prop,index) in entry.type.properties"
                            :class="index %2 == 0 ? 'bg-white' : 'bg-gray-50'">
                          <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                            {{prop.name}}
                          </td>
                          <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                            {{ typeName2(prop.type, prop.genericArgs) }}
                          </td>
                          <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                            {{isRequired(prop) ? 'Yes' : 'No'}}
                          </td>
                          <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                            {{prop.description}}
                          </td>
                        </tr>
                        </tbody>
                      </table>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
        
        <div v-if="routes.detailSrc" id="detail-src" class="flex-1 w-full lg:w-1/2 lg:bg-gray-50 lg:shadow-lg overflow-auto md:pb-scroll">
          <div v-if="store.activeDetailSrc">
            <CloseButton button-class="bg-gray-50" @close="routes.to({ detailSrc:'' })" />
            <pre class="pl-4 py-2"><code lang="csharp" v-highlightjs="store.activeDetailSrc.result"></code></pre>
          </div>
          <Loading v-else />
        </div>
    </div>
    `,
    setup() {

        const app = inject('app')
        const server = inject('server')
        const store = inject('store')
        const routes = inject('routes')
        const breakpoints = inject('breakpoints')
        
        const { typeOf, typeOfRef, typeProperties, typeName, typeName2 } = useMetadata()
        
        function addReferencedTypes(allTypesMap, type) {
            if (!type) return
            if (allTypesMap[type.name]) return

            /**: don't add generic types to allTypesMap, only generic arg references */
            let genericDef = type.name.indexOf('`') >= 0
            if (!genericDef) {
                allTypesMap[type.name] = type
            }
            if (type.genericArgs) {
                type.genericArgs.forEach(argType => {
                    addReferencedTypes(allTypesMap, typeOf(argType))
                })
            }
            if (type.inherits) {
                addReferencedTypes(allTypesMap, typeOfRef(type.inherits))
                if (type.inherits.genericArgs) {
                    type.inherits.genericArgs.forEach(argType => {
                        addReferencedTypes(allTypesMap, typeOf(argType))
                    })
                }
            }
            typeProperties(type).forEach(prop => {
                addReferencedTypes(allTypesMap, typeOf(prop.type))
                if (type.genericArgs) {
                    type.genericArgs.forEach(argType => {
                        addReferencedTypes(allTypesMap, typeOf(argType))
                    })
                }
            })
            return allTypesMap
        }

        const op = computed(() => store.op)
        const request = computed(() => store.op?.request)
        const opName = computed(() => store.op?.request?.name)
        
        function predefinedRoute(opName) {
            let apiFmt = server.httpHandlers['ApiHandlers.Json']
                || (server.plugins.loaded.indexOf('autoroutes') >= 0 ? '/json/reply/{Request}' : '')
            return apiFmt ? apiFmt.replace('{Request}', opName) : ''
        }
        
        function groupTypes(allTypes) {
            let allTypesMap = {}
            let groups = []
            allTypes.forEach(type => {
                if (allTypesMap[type.name]) return
                let group = []
                let addTypeDef = typeDef => {
                    if (!typeDef || allTypesMap[typeDef.name]) return
                    allTypesMap[typeDef.name] = typeDef
                    group.push({ type: typeDef, typeName: typeName(typeDef) })
                }
                let addTypeRef = typeRef => {
                    if (!typeRef || allTypesMap[typeRef.name]) return
                    let typeDef = typeOfRef(typeRef)
                    allTypesMap[typeDef.name] = typeDef
                    group.push({ type: typeDef, typeName: typeName(typeRef) })
                    return typeDef
                }
                addTypeDef(type)
                if (type.inherits) {
                    let subType = addTypeRef(type.inherits)
                    while (subType) {
                        subType = subType.inherits ? addTypeRef(subType.inherits) : null
                    }
                }
                groups.push(group)
            })
            return groups
        }
        
        function opReferencedTypes(op) {
            let allTypesMap = {}
            addReferencedTypes(allTypesMap, op.request)
            if (op.returnType) {
                /**: QueryResponse<'T> => QueryResponse<Model> */
                addReferencedTypes(allTypesMap, {...op.response, ...op.returnType})
            }
            let opTypes = Object.values(allTypesMap)
                .filter(type => !type.isValueType && type.name.toLowerCase() !== 'string')

            return groupTypes(opTypes)
        }
        
        function inheritedTypes(typeRef) {
            if (!typeRef) return ''
            let types = [typeRef.name]
            let metaType = typeOfRef(typeRef)
            while (metaType != null && metaType.inherits != null) {
                types.push(metaType.inherits.name)
                metaType = typeOfRef(metaType.inherits)
            }
            return types.join(',')
        }
        
        function apiDocs(typeName) {
            let name = typeName + 'Docs'
            return app.component(name)
        }
        
        function isRequired(prop) {
            return prop.isRequired || (prop.isValueType && prop.type !== 'Nullable`1')
        }

        return {
            store, routes, breakpoints,
            hasItems,
            op,
            request,
            opName,
            predefinedRoute,
            opReferencedTypes,
            inheritedTypes,
            apiDocs,
            isRequired,
            typeName,
            typeName2,
        }
        
    }
}
