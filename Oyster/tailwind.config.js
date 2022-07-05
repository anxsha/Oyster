module.exports = {
    content: [
        './Pages/**/*.cshtml',
        './Areas/Identity/Pages/**/*.cshtml',
        "./node_modules/flowbite/**/*.js"
    ],
    safelist: [
        // Flowbite safelist
        'w-64',
        'w-1/2',
        'rounded-l-lg',
        'rounded-r-lg',
        'bg-gray-200',
        'grid-cols-4',
        'grid-cols-7',
        'h-6',
        'leading-6',
        'h-9',
        'leading-9',
        'shadow-lg'
    ],
    theme: {
        colors: {
            primary: "#806BA2",
            lighterprimary1: "#9b85be",
            lighterprimary2: "#b7a8cd",
            primarybackground: "#d3cbde",
            info: "#4a2d3d",
            success: "#50a464",
            warning: "#cd942c",
            danger: "#f44336",
            lightshades: "#EBECEE",
            lightaccent: "#8E91A9",
            darkaccent: "#767581",
            darkshades: "#314C70",
            lighterdarkshades: "#5f86b9"
        },
        fontFamily: {
            sans: ['Roboto', 'sans-serif'],
            mono: ['Roboto mono', 'monospace']
        },
        extend: {
            keyframes: {
                slideInDown: {
                    '0%': { top: '-500px', opacity: '0.3' },
                    '100%': { top: '0', opacity: '1' },
                    
                },
                slideInUp: {
                    '0%': { bottom: '-500px', opacity: '0.3' },
                    '100%': { bottom: '0', opacity: '1' },
                    
                },
                hide: {
                    '0%': {visibility: 'visible', opacity: '1'},
                    '100%': {visibility: 'hidden', opacity: '0', height: '0', margin: '0', padding: '0'}
                }
            },
            animation: {
                // Both slideIns are used for displaying newly loaded comments
                slideInDown: 'slideInDown 1.5s cubic-bezier(.39,.58,.57,1)',
                slideInUp: 'slideInUp 0.8s cubic-bezier(.39,.58,.57,1)',
                // Used for hiding alerts (error, info...)
                hide: 'hide 1s linear' 
            }
        }
    },
    variants: {
        extend: {},
    },
    plugins: [
        require('flowbite/plugin')
    ],
}
