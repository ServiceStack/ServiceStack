module.exports = {
  content: [
    './ui/**/*.html',
    './admin-ui/**/*.{html,mjs}',
    './locode/**/*.{html,mjs}', 
    './shared/**/*.html', 
    './custom/**/*.html',
    './locode-v1/**/*.html',
    './wwwroot/**/*.{html,mjs}',
    '../../src/ServiceStack/js/servicestack-vue.mjs'
  ],
  theme: {
    extend: {},
  },
  plugins: [
    require('@tailwindcss/forms')
  ],
}
