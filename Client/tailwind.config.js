/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,jsx}'],
  theme: {
    extend: {
      colors: {
        ink: '#050510',
        panel: '#0a0a1a',
        panelSoft: '#0f0f24',
        line: '#1a1a35',
        mist: '#8888aa',
        neon: {
          cyan: '#00e5ff',
          purple: '#b347ea',
          green: '#39ff14',
          pink: '#ff006e',
          amber: '#ffab00',
          blue: '#2979ff'
        }
      },
      boxShadow: {
        glow: '0 0 0 1px rgba(0,229,255,0.06), 0 12px 30px rgba(0,0,0,0.4)',
        'neon-cyan': '0 0 8px rgba(0,229,255,0.06)',
        'neon-purple': '0 0 8px rgba(179,71,234,0.06)',
        'neon-green': '0 0 8px rgba(57,255,20,0.06)'
      }
    }
  },
  plugins: []
};
