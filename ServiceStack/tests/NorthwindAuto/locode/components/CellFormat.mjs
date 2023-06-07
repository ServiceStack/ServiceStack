/**: override's @servicestack/vue `CellFormat` to linkify related results */ 
import { h, inject } from "vue"
import ServiceStackVue, { useMetadata } from "@servicestack/vue"
import { mapGet } from "@servicestack/client"

export const CellFormat = {
    props:['type','propType','modelValue'],
    setup(props) {
        return () => {
            const originalComponent = ServiceStackVue.component('CellFormat')
            const { type, propType, modelValue } = props
            const hOrig = h(originalComponent, { type, propType, modelValue })

            const ref = props.propType?.ref
            if (!ref?.model)
                return hOrig

            const { Apis, isComplexProp } = useMetadata()
            
            const apis = Apis.forType(ref.model)
            if (!apis.AnyQuery)
                return hOrig

            const routes = inject('routes')
            const value = mapGet(modelValue, propType.name)
            
            return h('span', { 
                'class':'text-blue-600 hover:text-blue-800 cursor-pointer',
                title:`${ref.model} ${value}`,
                onClick: (e) => {
                    e.stopPropagation()
                    const $qs = {}
                    if (ref.selfId) {
                        const selfId = mapGet(modelValue, ref.selfId)
                        $qs[ref.refId] = selfId 
                    } else {
                        if (isComplexProp(propType)) {
                            const refValue = mapGet(value, ref.refId)
                            $qs[ref.refId] = refValue
                        } else {
                            $qs[ref.refId] = value
                        }
                    }
                    routes.to({ op: apis.AnyQuery.request.name, $qs })
                }
            }, [
                hOrig
            ])
        }
    }
}