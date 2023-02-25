import { inject, ref } from "vue"
import { app } from "app"
import { QueryCoupons } from "/types/mjs"

const BookingGrid = {
    template:/*html*/`
      <div>
          <h1 class="py-2 text-2xl font-semibold">Custom Bookings AutoQueryGrid</h1>
          <AutoQueryGrid type="Booking" selected-columns="id,name,cost,bookingStartDate,bookingEndDate,discount,createdBy">
            <template #discount="{ discount }">
              <TextLink v-if="discount" class="flex items-end" @click.stop="showCoupon(discount.id)" :title="discount.id">
                <Icon class="w-5 h-5 mr-1" type="Coupon" />
                <PreviewFormat :value="discount.description" />
              </TextLink>
            </template>
          </AutoQueryGrid>
          <AutoEditForm v-if="coupon" type="UpdateCoupon" v-model="coupon" @done="close" @save="close" />
      </div>
    `,
    props:['type'],
    setup() {
        const client = inject('client')
        const coupon = ref()
        async function showCoupon(id) {
            const api = await client.api(new QueryCoupons({ id }))
            if (api.succeeded) {
                coupon.value = api.response.results[0]
            }
        }
        const close = () => coupon.value = null
        return { coupon, showCoupon, close }
    }
}

//app.components({ BookingGrid })