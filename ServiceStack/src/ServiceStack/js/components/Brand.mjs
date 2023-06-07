export const Brand = {
    template:/*html*/`
      <div class="flex items-center flex-shrink-0 max-w-sidebar">
          <a :title="name" v-href="{ $page:'' }"
             class="text-2xl whitespace-nowrap overflow-x-hidden flex items-center">
            <Icon v-if="icon" class="brand-icon w-8 h-8 mr-1" :image="icon" alt="logo" />
            {{name}}
          </a>
      </div>
    `,
    props: ['icon','name']
}
export default Brand
