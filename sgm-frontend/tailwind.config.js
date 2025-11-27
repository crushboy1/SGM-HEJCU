/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      // ===================================================================
      // COLORES DEL SISTEMA HOSPITALARIO
      // ===================================================================
      colors: {
        // Colores base 
        'hospital-cyan': '#03a9f3',
        'hospital-blue': '#0891B2',
        'hospital-blue-dark': '#0e7490',
        'hospital-bg': '#E8EEF3',

        // Estados 
        'hospital-green': '#28A745',
        'hospital-green-light': '#D4EDDA',
        'hospital-red': '#DC3545',
        'hospital-red-light': '#F8D7DA',
        'hospital-orange': '#FD7E14',
        'hospital-orange-light': '#FFE5D0',
        'hospital-yellow': '#FFC107',
        'hospital-yellow-light': '#FFF3CD',

        // SGM Específicos
        'sgm-purple': '#8B5CF6',
        'sgm-purple-light': '#EDE9FE',
        'sgm-dark': '#1F2937',
        'sgm-gray': '#6B7280',

        // Paleta extendida
        cyan: {
          50: '#ECFEFF',
          100: '#CFFAFE',
          200: '#A5F3FC',
          300: '#67E8F9',
          400: '#22D3EE',
          500: '#06B6D4',
          600: '#0891B2',
          700: '#0E7490',
          800: '#155E75',
          900: '#164E63',
        },
        purple: {
          50: '#FAF5FF',
          100: '#F3E8FF',
          200: '#E9D5FF',
          300: '#D8B4FE',
          400: '#C084FC',
          500: '#A855F7',
          600: '#9333EA',
          700: '#7E22CE',
          800: '#6B21A8',
          900: '#581C87',
        },
        
        orange: {
          50: '#FFF7ED',
          100: '#FFEDD5',
          200: '#FED7AA',
          300: '#FDBA74',
          400: '#FB923C',
          500: '#F97316',
          600: '#EA580C',
          700: '#C2410C',
          800: '#9A3412',
          900: '#7C2D12',
        },
      },

      // ===================================================================
      // ANIMACIONES MODERNAS
      // ===================================================================
      animation: {
        'fade-in': 'fadeIn 0.5s ease-in-out',
        'fade-in-up': 'fadeInUp 0.6s ease-out',
        'slide-in-right': 'slideInRight 0.5s ease-out',
        'slide-in-left': 'slideInLeft 0.5s ease-out',
        'pulse-soft': 'pulseSoft 2s infinite',
        'pulse-once': 'pulseOnce 0.6s ease-out',
        'bounce-gentle': 'bounceGentle 1s ease-in-out infinite',
        'card-hover': 'cardHover 0.3s ease-out',
        'shimmer': 'shimmer 2s infinite linear',
        'spin-slow': 'spin 2s linear infinite',
      },

      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        fadeInUp: {
          '0%': { opacity: '0', transform: 'translateY(20px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        slideInRight: {
          '0%': { opacity: '0', transform: 'translateX(30px)' },
          '100%': { opacity: '1', transform: 'translateX(0)' },
        },
        slideInLeft: {
          '0%': { opacity: '0', transform: 'translateX(-30px)' },
          '100%': { opacity: '1', transform: 'translateX(0)' },
        },
        pulseSoft: {
          '0%, 100%': { opacity: '1' },
          '50%': { opacity: '0.7' },
        },
        // Pulso único
        pulseOnce: {
          '0%': { transform: 'scale(1)', opacity: '1' },
          '50%': { transform: 'scale(1.05)', opacity: '0.8' },
          '100%': { transform: 'scale(1)', opacity: '1' },
        },
        bounceGentle: {
          '0%, 100%': { transform: 'translateY(0)' },
          '50%': { transform: 'translateY(-8px)' },
        },
        cardHover: {
          '0%': { transform: 'translateY(0) scale(1)' },
          '100%': { transform: 'translateY(-4px) scale(1.01)' },
        },
        shimmer: {
          '0%': { backgroundPosition: '-200% 0' },
          '100%': { backgroundPosition: '200% 0' },
        },
      },

      boxShadow: {
        'soft': '0 2px 15px -3px rgba(0, 0, 0, 0.07), 0 10px 20px -2px rgba(0, 0, 0, 0.04)',
        'medium': '0 4px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 30px -5px rgba(0, 0, 0, 0.05)',
        'card': '0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06)',
        'card-hover': '0 10px 30px -10px rgba(0, 0, 0, 0.15)',
        'inner-soft': 'inset 0 2px 4px 0 rgba(0, 0, 0, 0.06)',
      },

      fontSize: {
        '2xs': ['0.625rem', { lineHeight: '0.75rem' }],
      },

      spacing: {
        '18': '4.5rem',
        '88': '22rem',
      },

      borderRadius: {
        '4xl': '2rem',
      },
    },
  },

  plugins: [
    function ({ addUtilities, theme }) {
      addUtilities({

        // ===================================================================
        // BADGES BASE
        // ===================================================================
        '.badge': {
          '@apply inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold border transition-all duration-200': {},
        },
        '.badge-lg': {
          '@apply inline-flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-semibold border-2 transition-all duration-200': {},
        },

        // ===================================================================
        // CARDS
        // ===================================================================
        '.card': {
          '@apply bg-white rounded-lg shadow-card border border-gray-100 transition-all duration-300': {},
        },
        '.card-hover': {
          '@apply hover:shadow-card-hover hover:-translate-y-1': {},
        },

        // ===================================================================
        // BOTONES
        // ===================================================================
        '.btn-primary': {
          '@apply bg-hospital-blue hover:bg-hospital-blue-dark text-white font-semibold py-2 px-4 rounded-lg shadow-soft transition-all duration-200 hover:shadow-medium active:scale-95': {},
        },
        '.btn-secondary': {
          '@apply bg-white hover:bg-gray-50 text-gray-700 font-semibold py-2 px-4 rounded-lg border border-gray-300 shadow-soft transition-all duration-200 active:scale-95': {},
        },
        '.btn-success': {
          '@apply bg-hospital-green hover:bg-green-700 text-white font-semibold py-2 px-4 rounded-lg shadow-soft transition-all duration-200 active:scale-95': {},
        },
        '.btn-danger': {
          '@apply bg-hospital-red hover:bg-red-700 text-white font-semibold py-2 px-4 rounded-lg shadow-soft transition-all duration-200 active:scale-95': {},
        },

        // ===================================================================
        // GLASSMORPHISM
        // ===================================================================
        '.glass': {
          '@apply bg-white/90 backdrop-blur-md border border-white/20': {},
        },

        // ===================================================================
        // SCROLLBAR PERSONALIZADO
        // ===================================================================
        '.scrollbar-custom': {
          'scrollbar-width': 'thin',
          'scrollbar-color': `${theme('colors.hospital-blue')} ${theme('colors.gray.200')}`,
          '&::-webkit-scrollbar': {
            width: '8px',
          },
          '&::-webkit-scrollbar-track': {
            background: theme('colors.gray.100'),
            borderRadius: '10px',
          },
          '&::-webkit-scrollbar-thumb': {
            background: theme('colors.hospital-blue'),
            borderRadius: '10px',
            '&:hover': {
              background: theme('colors.hospital-blue-dark'),
            },
          },
        },

        // ===================================================================
        // DISPLAY CORRECTO EN ÍCONOS
        // ===================================================================
        '.icon-inline': {
          'display': 'inline-flex !important',
          'align-items': 'center',
          'justify-content': 'center',
          'line-height': '0',
          '& svg': {
            'display': 'block !important',
            'width': '100%',
            'height': '100%',
          },
        },
        
        // ===================================================================
        // ANIMATION DELAY HELPERS (Para stagger de KPIs)
        // ===================================================================
        '.animate-delay-100': {
          'animation-delay': '0.1s',
        },
        '.animate-delay-200': {
          'animation-delay': '0.2s',
        },
        '.animate-delay-300': {
          'animation-delay': '0.3s',
        },
        '.animate-delay-400': {
          'animation-delay': '0.4s',
        },
        '.animate-delay-600': {
          'animation-delay': '0.6s',
        },
        // ===================================================================
        // ESTILOS DE TARJETAS DASHBOARD (KPIs)
        // ===================================================================
        '.kpi-card': {
          '@apply bg-white rounded-xl border-l-4 shadow-sm hover:shadow-md transition-all duration-300 p-0 overflow-hidden relative': {},
        },
        '.kpi-card-content': {
          '@apply p-5 z-10 relative': {},
        },
        '.kpi-card-label': {
          '@apply text-xs font-bold text-gray-500 uppercase tracking-wide mb-1': {},
        },
        '.kpi-card-value': {
          '@apply text-3xl font-bold': {},
        },
        '.kpi-card-icon': {
          '@apply p-3 rounded-xl transition-transform duration-300 hover:scale-110': {},
        },
        '.kpi-card-footer': {
          '@apply flex items-center gap-2 text-xs text-gray-500 mt-3 pt-3 border-t border-gray-50': {},
        },
        '.kpi-card-dot': {
          '@apply h-2 w-2 rounded-full': {},
        },
        // Variantes KPI
        '.kpi-card-green': { '@apply border-green-500': {} },
        '.kpi-card-blue': { '@apply border-blue-500': {} },
        '.kpi-card-yellow': { '@apply border-yellow-500': {} },
        '.kpi-card-purple': { '@apply border-purple-500': {} },

        // ===================================================================
        // ESTADÍSTICAS MORTUORIO (Stat Cards)
        // ===================================================================
        '.stat-card': {
          '@apply text-center p-4 rounded-xl border-2 transition-all cursor-default hover:shadow-sm bg-white': {},
        },
        '.stat-card-value': {
          '@apply text-2xl md:text-3xl font-bold mb-1': {},
        },
        '.stat-card-label': {
          '@apply text-xs text-gray-500 uppercase font-bold tracking-wide flex justify-center items-center gap-1': {},
        },
        // Variantes Stat Cards
        '.stat-card-green': { '@apply bg-green-50 border-green-100 hover:border-green-300': {} },
        '.stat-card-blue': { '@apply bg-blue-50 border-blue-100 hover:border-blue-300': {} },
        '.stat-card-gray': { '@apply bg-gray-50 border-gray-100 hover:border-gray-300': {} },
        '.stat-card-orange': { '@apply bg-orange-50 border-orange-100 hover:border-orange-300': {} },

        // ===================================================================
        // TARJETAS DE ACCIÓN (Action Cards)
        // ===================================================================
        '.action-card': {
          '@apply bg-white rounded-xl border-l-4 shadow-sm hover:shadow-lg transition-all duration-300 cursor-pointer text-left relative overflow-hidden': {},
        },
        '.action-card-content': {
          '@apply p-6 z-10 relative': {},
        },
        '.action-card-title': {
          '@apply text-lg font-bold text-gray-800 transition-colors': {},
        },
        '.action-card-description': {
          '@apply text-gray-500 text-sm mt-2 leading-relaxed': {},
        },
        '.action-card-icon': {
          '@apply p-3 rounded-full transition-transform duration-300': {},
        },
        // Variantes Action Cards
        '.action-card-cyan': { '@apply border-cyan-500 hover:border-cyan-600': {} },
        '.action-card-green': { '@apply border-green-500 hover:border-green-600': {} },
        '.action-card-blue': { '@apply border-blue-500 hover:border-blue-600': {} },
      });
    },
  ],
}
