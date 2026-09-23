/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{vue,js,ts}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['DM Sans', 'sans-serif'],
        mono: ['JetBrains Mono', 'monospace'],
      },
      colors: {
        brand: {
          50:  '#eef5ff',
          100: '#d9e9ff',
          200: '#bcd7ff',
          300: '#8cbeff',
          400: '#559eff',
          500: '#2b7fff',
          600: '#1462f5',
          700: '#0d4de1',
          800: '#1040b6',
          900: '#133b8f',
        },
        surface: {
          0:   '#ffffff',
          50:  '#f8f9fc',
          100: '#f0f2f7',
          200: '#e4e7ef',
          300: '#d1d6e3',
        },
        ink: {
          900: '#0f1523',
          700: '#2d3650',
          500: '#5a6581',
          300: '#9aa2b8',
          100: '#d4d9e8',
        }
      },
      borderRadius: {
        DEFAULT: '10px',
        lg: '14px',
        xl: '18px',
      },
      boxShadow: {
        card: '0 1px 3px rgba(15,21,35,0.06), 0 4px 16px rgba(15,21,35,0.04)',
        float: '0 8px 32px rgba(15,21,35,0.12)',
      }
    },
  },
  plugins: [],
}
