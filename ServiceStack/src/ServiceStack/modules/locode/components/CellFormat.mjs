import { h, inject } from "vue"
import ServiceStackVue, { useMetadata } from "@servicestack/vue"
import { mapGet } from "@servicestack/client"
//NOTE: Keep outside of component otherwise fails with 'RangeError: Maximum call stack size exceeded'
const originalComponent = ServiceStackVue.component('CellFormat')
export const CellFormat = {
    props:['type','propType','modelValue'],
    setup(props) {
        const router = inject('router')
        return () => {
            const { type, propType, modelValue } = props
            const hOrig = h(originalComponent, { type, propType, modelValue })
            const ref = props.propType?.ref
            if (!ref?.model)
                return hOrig
            const { Apis, isComplexProp } = useMetadata()
            const apis = Apis.forType(ref.model)
            if (!apis.AnyQuery)
                return hOrig
            const value = mapGet(modelValue, propType.name)
            return h('span', {
                'class':'text-blue-600 hover:text-blue-800 cursor-pointer',
                title:`${ref.model} ${value}`,
                onClick: (e) => {
                    e.stopPropagation()
                    const query = {}
                    if (ref.selfId) {
                        const selfId = mapGet(modelValue, ref.selfId)
                        query[ref.refId] = selfId
                    } else {
                        if (isComplexProp(propType)) {
                            const refValue = mapGet(value, ref.refId)
                            query[ref.refId] = refValue
                        } else {
                            query[ref.refId] = value
                        }
                    }
                    router.push({
                        name: 'operation',
                        params: { op: apis.AnyQuery.request.name },
                        query
                    })
                }
            }, [
                hOrig
            ])
        }
    }
}
