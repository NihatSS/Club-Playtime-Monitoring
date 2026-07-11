/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,jsx}'],
  theme: {
    extend: {
      colors: {
        ink: '#09090b',
        panel: '#121217',
        panelSoft: '#18181f',
        line: '#2a2a33',
        mist: '#a1a1aa'
      },
      boxShadow: {
        glow: '0 0 0 1px rgba(255,255,255,0.05), 0 18px 50px rgba(0,0,0,0.35)'
      }
    }
  },
  plugins: []
};
