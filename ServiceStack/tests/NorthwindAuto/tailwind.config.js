module.exports = {
  content: ['./ui/**/*.html', './admin-ui/**/*.html', './locode/**/*.html', './shared/**/*.html', './custom/**/*.html'],
  theme: {
    extend: {},
  },
  plugins: [
    require('@tailwindcss/forms')
  ],
}
