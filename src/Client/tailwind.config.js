/** @type {import('tailwindcss').Config} */

const config = {
  content: [
    "./index.html",
    "./src/**/*.{fs,fsx,ts,tsx,js,jsx}"
  ],
  darkMode: 'class', // Enable class-based dark mode
  theme: {
    extend: {}
  },
  plugins: [],
};

export default config;
