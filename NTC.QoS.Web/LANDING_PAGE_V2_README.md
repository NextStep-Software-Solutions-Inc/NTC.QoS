# Landing Page Version 2 - Complete Redesign

## Overview

A completely redesigned landing page for the NTC QoS platform featuring modern animations, custom illustrations, and enhanced user experience while maintaining the same color palette.

## What Changed

### Version 1 (Preserved)

- Original landing page saved as `src/app/page-v1.tsx` and `src/app/page-v1.css`
- All original sections remain intact and functional
- Can be restored by renaming files back to `page.tsx` and `page.css`

### Version 2 (Current)

Complete redesign with new sections and modern aesthetics.

## New Features

### 🎨 Custom Illustrations

Located in `src/components/v2/illustrations/`:

1. **NetworkIllustration.tsx** - Animated canvas network visualization
   - Particle nodes with random movement
   - Dynamic connection lines based on proximity
   - Smooth animations and responsive sizing

2. **MapIllustration.tsx** - SVG-based Philippine map visualization
   - Simplified archipelago outline
   - Animated connectivity nodes
   - Pulsing signal waves
   - Gradient effects

3. **DataFlowIllustration.tsx** - Data transfer visualization
   - Server/device representations
   - Animated data packets
   - Flow path animations
   - Bidirectional data streams

### 📱 New Sections

Located in `src/components/v2/sections/`:

1. **HeroSectionV2.tsx**
   - Gradient background with animated blobs
   - Interactive network illustration overlay
   - Prominent CTA buttons
   - Live stats display (50K+ sites, 1M+ tests, 100% coverage)
   - Floating info cards
   - Scroll indicator animation
   - **Mobile optimized**: Stacked layout, responsive text sizes

2. **FeaturesSectionV2.tsx**
   - 4 key features with icons
   - Intersection Observer for scroll animations
   - Staggered card reveals
   - Hover effects with gradient borders
   - Icons: MapPin, BarChart3, Radio, Shield
   - **Mobile optimized**: Single column on mobile, 2 columns on tablet+

3. **StatsSectionV2.tsx**
   - Gradient blue-to-purple background
   - Animated counter numbers
   - 4 statistics with icons
   - Background pattern overlay
   - Glass morphism effect on cards
   - **Mobile optimized**: 2 columns on mobile, 4 on desktop

4. **DataInsightsSectionV2.tsx**
   - Dark theme section (gray-900 to blue-900)
   - 4 insight cards with real-time data
   - DataFlow illustration integration
   - Floating stat badges
   - Glow effects
   - **Mobile optimized**: Stacked layout with full-width cards

5. **CTASectionV2.tsx**
   - Two-column layout with content + illustration
   - Benefits checklist with CheckCircle2 icons
   - Multiple CTA buttons
   - Floating badges
   - MapIllustration integration
   - **Mobile optimized**: Single column on mobile

6. **FAQSectionV2.tsx**
   - Accordion-style FAQ component
   - Smooth expand/collapse animations
   - 6 common questions
   - Contact support CTA
   - Intersection Observer animations
   - **Mobile optimized**: Full-width accordions with touch-friendly targets

### ✨ Animations

Enhanced `src/app/page.css` with new keyframes:

- `blob` - Organic blob movement (7s infinite)
- `float` - Gentle vertical floating (3s infinite)
- `scroll` - Scroll indicator animation (2s infinite)
- `fadeInUp` - Fade in from bottom (0.6s)
- `fadeInDown` - Fade in from top (0.6s)
- `slideInLeft` - Slide in from left (0.6s)
- `slideInRight` - Slide in from right (0.6s)
- `scaleIn` - Scale up animation (0.6s)

Animation utilities:

- `.animate-blob` - Blob animation
- `.animate-float` - Floating animation
- `.animate-scroll` - Scroll animation
- `.animation-delay-{100-500}` - Staggered delays
- `.animation-delay-{2000,4000}` - Long delays

### 🎯 SEO & Accessibility

All sections include:

- Semantic HTML5 elements (`<section>`, `<h1-h6>`, `<nav>`)
- ARIA labels on interactive elements
- Alt text on all visual elements
- Proper heading hierarchy
- Keyboard navigation support
- Focus indicators
- Screen reader friendly text
- Reduced motion support via media query

### 📱 Mobile Responsiveness

All components are fully responsive:

- Mobile-first approach
- Breakpoints: sm (640px), md (768px), lg (1024px)
- Flexible grids that stack on mobile
- Touch-friendly button sizes (min 44px)
- Optimized font sizes for readability
- Proper spacing on all screen sizes
- Hidden overflow prevention

### 🎨 Design System

**Color Palette** (maintained from v1):

- Primary: Blue-600 to Blue-700 (`#2563eb` to `#1d4ed8`)
- Secondary: Purple-600 (`#9333ea`)
- Backgrounds: Gray-50, Blue-50, Purple-50
- Gradients: Blue to Purple combinations

**Typography**:

- Headings: Bold, 3xl to 5xl on mobile/desktop
- Body: Regular, text-base to lg
- Font: Geist Sans (via Tailwind config)

**Spacing**:

- Section padding: py-20 lg:py-32
- Container: max-w-7xl with responsive px
- Gaps: 4-8 for tight spacing, 12-16 for sections

### 🚀 Performance

- Intersection Observer for lazy animations
- Canvas animations with requestAnimationFrame
- CSS animations over JavaScript when possible
- Optimized SVGs
- Conditional rendering based on visibility
- Reduced motion support for accessibility

## File Structure

```
src/
├── app/
│   ├── page.tsx           # Version 2 (current)
│   ├── page-v1.tsx        # Version 1 (backup)
│   ├── page.css           # Version 2 animations
│   └── page-v1.css        # Version 1 animations (backup)
├── components/
│   └── v2/
│       ├── illustrations/
│       │   ├── NetworkIllustration.tsx
│       │   ├── MapIllustration.tsx
│       │   └── DataFlowIllustration.tsx
│       └── sections/
│           ├── HeroSectionV2.tsx
│           ├── FeaturesSectionV2.tsx
│           ├── StatsSectionV2.tsx
│           ├── DataInsightsSectionV2.tsx
│           ├── CTASectionV2.tsx
│           └── FAQSectionV2.tsx
```

## How to Switch Between Versions

### To use Version 1

```bash
mv src/app/page.tsx src/app/page-v2.tsx
mv src/app/page-v1.tsx src/app/page.tsx
mv src/app/page.css src/app/page-v2.css
mv src/app/page-v1.css src/app/page.css
```

### To use Version 2 (current)

Already active - no changes needed.

## Development

```bash
# Install dependencies
pnpm install

# Run dev server
pnpm dev

# Build for production
pnpm build

# Start production server
pnpm start
```

## Browser Support

- Chrome/Edge: Latest 2 versions
- Firefox: Latest 2 versions
- Safari: Latest 2 versions
- Mobile Safari: iOS 12+
- Chrome Mobile: Latest

## Change Log

### Version 2.0.0 (December 2025)

- ✅ Complete landing page redesign
- ✅ 3 custom SVG/Canvas illustrations
- ✅ 6 new section components
- ✅ Advanced CSS animations and keyframes
- ✅ Intersection Observer scroll animations
- ✅ Animated counters and stats
- ✅ Mobile-responsive design (mobile-first)
- ✅ SEO optimized with semantic HTML
- ✅ Accessibility compliant (WCAG 2.1 AA)
- ✅ Reduced motion support
- ✅ TypeScript strict mode compatible
- ✅ Production build tested and verified

## Credits

- Design: Custom redesign for NTC QoS platform
- Illustrations: Custom SVG and Canvas animations
- Icons: Lucide React
- Framework: Next.js 16 with TypeScript
- Styling: Tailwind CSS
