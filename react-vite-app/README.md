# React + Vite + TailwindCSS

A minimal and modern React application built with the latest dependencies and recommended best practices.

## 🚀 Tech Stack

- **React 19.2** - Latest version of React with improved performance
- **Vite 7.2** - Next-generation frontend tooling with lightning-fast HMR
- **TailwindCSS 4.1** - Modern utility-first CSS framework with CSS-based configuration
- **ESLint 9** - Latest ESLint with flat config format
- **Prettier 3** - Opinionated code formatter

## 📦 Features

- ⚡ Lightning-fast development with Vite HMR
- 🎨 TailwindCSS v4 with CSS-based configuration
- 🔧 Modern ESLint configuration (flat config)
- 💅 Prettier for consistent code formatting
- 📱 Responsive design out of the box
- 🏗️ Optimized production build with code splitting

## 🛠️ Getting Started

### Prerequisites

- Node.js 18+ and npm 10+

### Installation

```bash
# Install dependencies
npm install
```

### Development

```bash
# Start development server
npm run dev
```

The app will open automatically at `http://localhost:3000`

### Build

```bash
# Create production build
npm run build
```

### Preview

```bash
# Preview production build
npm run preview
```

### Linting & Formatting

```bash
# Run ESLint
npm run lint

# Format code with Prettier
npm run format
```

## 📁 Project Structure

```
react-vite-app/
├── src/
│   ├── App.jsx          # Main App component
│   ├── main.jsx         # Application entry point
│   └── index.css        # Global styles with Tailwind imports
├── index.html           # HTML entry point
├── vite.config.js       # Vite configuration
├── postcss.config.js    # PostCSS configuration
├── eslint.config.js     # ESLint configuration (flat config)
├── .prettierrc          # Prettier configuration
└── package.json         # Dependencies and scripts
```

## 🎯 Best Practices

- **ES Modules**: Uses modern ES module syntax throughout
- **Strict Mode**: React StrictMode enabled for highlighting potential problems
- **Code Splitting**: Automatic code splitting for React vendor bundle
- **Hot Module Replacement**: Fast refresh for instant feedback during development
- **Modern ESLint**: Uses the new flat config format introduced in ESLint 9
- **TailwindCSS v4**: Leverages the new CSS-based configuration system

## 📝 Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build locally
- `npm run lint` - Run ESLint
- `npm run format` - Format code with Prettier

## 🔗 Resources

- [Vite Documentation](https://vite.dev/)
- [React Documentation](https://react.dev/)
- [TailwindCSS Documentation](https://tailwindcss.com/)
- [ESLint Documentation](https://eslint.org/)
