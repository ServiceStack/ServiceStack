module.exports = {
  content: [
    './ui/**/*.html', 
    './admin-ui/**/*.html', 
    './locode/**/*.html', 
    './shared/**/*.html', 
    './custom/**/*.html',
    './locode2/**/*.html',
    '../../src/ServiceStack/js/servicestack-vue.mjs'
  ],
  theme: {
    extend: {},
  },
  plugins: [
    require('@tailwindcss/forms')
  ],
}
