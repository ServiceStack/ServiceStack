module.exports = {
  content: ['./ui/**/*.html', './admin-ui/**/*.html', './shared/**/*.html'],
  theme: {
    extend: {},
  },
  plugins: [
    require('@tailwindcss/forms')
  ],
}
