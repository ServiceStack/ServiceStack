import { useState } from 'react'

function App() {
  const [count, setCount] = useState(0)

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
      <div className="container mx-auto px-4 py-16">
        <header className="text-center mb-12">
          <h1 className="text-5xl font-bold text-gray-900 mb-4">React + Vite + TailwindCSS</h1>
          <p className="text-xl text-gray-600">
            A minimal and modern setup with the latest dependencies
          </p>
        </header>

        <main className="max-w-2xl mx-auto">
          <div className="bg-white rounded-2xl shadow-xl p-8 mb-8">
            <h2 className="text-2xl font-semibold text-gray-800 mb-4">Interactive Counter</h2>
            <div className="flex items-center justify-center gap-4">
              <button
                onClick={() => setCount((count) => count - 1)}
                className="px-6 py-3 bg-red-500 text-white font-semibold rounded-lg hover:bg-red-600 active:bg-red-700 transition-colors duration-200 shadow-md hover:shadow-lg"
              >
                Decrease
              </button>
              <div className="text-4xl font-bold text-indigo-600 min-w-[100px] text-center">
                {count}
              </div>
              <button
                onClick={() => setCount((count) => count + 1)}
                className="px-6 py-3 bg-green-500 text-white font-semibold rounded-lg hover:bg-green-600 active:bg-green-700 transition-colors duration-200 shadow-md hover:shadow-lg"
              >
                Increase
              </button>
            </div>
            <button
              onClick={() => setCount(0)}
              className="mt-4 w-full px-6 py-3 bg-gray-500 text-white font-semibold rounded-lg hover:bg-gray-600 active:bg-gray-700 transition-colors duration-200 shadow-md hover:shadow-lg"
            >
              Reset
            </button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <FeatureCard
              title="⚡ Vite"
              description="Lightning-fast development with Hot Module Replacement"
              color="bg-purple-500"
            />
            <FeatureCard
              title="⚛️ React 19"
              description="Latest React with improved performance and features"
              color="bg-blue-500"
            />
            <FeatureCard
              title="🎨 Tailwind v4"
              description="Modern utility-first CSS framework with CSS-based config"
              color="bg-cyan-500"
            />
          </div>
        </main>

        <footer className="text-center mt-16 text-gray-600">
          <p>
            Edit <code className="bg-gray-200 px-2 py-1 rounded">src/App.jsx</code> and save to test
            HMR
          </p>
        </footer>
      </div>
    </div>
  )
}

function FeatureCard({ title, description, color }) {
  return (
    <div className="bg-white rounded-xl shadow-lg p-6 hover:shadow-xl transition-shadow duration-200">
      <div className={`${color} text-white text-2xl font-bold rounded-lg p-3 mb-4 text-center`}>
        {title}
      </div>
      <p className="text-gray-600 text-center">{description}</p>
    </div>
  )
}

export default App
