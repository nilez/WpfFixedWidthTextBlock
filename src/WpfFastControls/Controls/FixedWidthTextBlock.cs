using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace WpfFastControls.Controls
{
    /// <summary>
    /// A render-only text display element with a fixed, pre-measured size.
    ///
    /// Problem solved: the standard WPF <see cref="System.Windows.Controls.TextBlock"/> registers
    /// its Text property with <c>AffectsMeasure | AffectsRender</c>. Every text change therefore
    /// calls <c>InvalidateMeasure()</c>, which walks upward through the visual tree invalidating
    /// parent panels, grandparent panels, and so on — triggering a full layout pass even though
    /// the displayed text occupies exactly the same pixel rectangle as before.
    ///
    /// Solution: this control registers <see cref="Text"/> with
    /// <c>FrameworkPropertyMetadataOptions.AffectsRender</c> only.  A text change calls
    /// <c>InvalidateVisual()</c> — a render-only dirtying that never propagates upward.
    /// <see cref="MeasureOverride"/> returns a fixed size (either the caller-supplied
    /// <see cref="FixedWidth"/>/<see cref="FixedHeight"/>, or the size measured on the first
    /// layout pass) so the layout system never needs to re-measure this element again.
    /// </summary>
    public class FixedWidthTextBlock : FrameworkElement
    {
        // ── Dependency properties ────────────────────────────────────────────

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text), typeof(string), typeof(FixedWidthTextBlock),
                new FrameworkPropertyMetadata(string.Empty,
                    FrameworkPropertyMetadataOptions.AffectsRender,   // render only — no layout pass
                    OnTextChanged));

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(
                nameof(Foreground), typeof(Brush), typeof(FixedWidthTextBlock),
                new FrameworkPropertyMetadata(Brushes.Black,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(
                nameof(FontSize), typeof(double), typeof(FixedWidthTextBlock),
                new FrameworkPropertyMetadata(14.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty FontWeightProperty =
            DependencyProperty.Register(
                nameof(FontWeight), typeof(FontWeight), typeof(FixedWidthTextBlock),
                new FrameworkPropertyMetadata(FontWeights.Normal,
                    FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register(
                nameof(FontFamily), typeof(FontFamily), typeof(FixedWidthTextBlock),
                new FrameworkPropertyMetadata(new FontFamily("Segoe UI"),
                    FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(
                nameof(TextAlignment), typeof(TextAlignment), typeof(FixedWidthTextBlock),
                new FrameworkPropertyMetadata(TextAlignment.Left,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Optional fixed width override. When set, <see cref="MeasureOverride"/> always
        /// returns this value and the element never re-measures due to text changes.
        /// </summary>
        public static readonly DependencyProperty FixedWidthProperty =
            DependencyProperty.Register(
                nameof(FixedWidth), typeof(double), typeof(FixedWidthTextBlock),
                new FrameworkPropertyMetadata(double.NaN,
                    FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Optional fixed height override. When set, <see cref="MeasureOverride"/> always
        /// returns this value and the element never re-measures due to text changes.
        /// </summary>
        public static readonly DependencyProperty FixedHeightProperty =
            DependencyProperty.Register(
                nameof(FixedHeight), typeof(double), typeof(FixedWidthTextBlock),
                new FrameworkPropertyMetadata(double.NaN,
                    FrameworkPropertyMetadataOptions.AffectsMeasure));

        // ── CLR wrappers ─────────────────────────────────────────────────────

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        public double FixedWidth
        {
            get => (double)GetValue(FixedWidthProperty);
            set => SetValue(FixedWidthProperty, value);
        }

        public double FixedHeight
        {
            get => (double)GetValue(FixedHeightProperty);
            set => SetValue(FixedHeightProperty, value);
        }

        // ── State ─────────────────────────────────────────────────────────────

        private FormattedText? _formatted;
        private Typeface? _typeface;
        private double _pixelsPerDip;

        // ── Callbacks ─────────────────────────────────────────────────────────

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // AffectsRender already calls InvalidateVisual(); we rebuild FormattedText
            // here so OnRender stays allocation-free on each frame.
            ((FixedWidthTextBlock)d).RebuildFormattedText();
        }

        // ── Layout ────────────────────────────────────────────────────────────

        protected override Size MeasureOverride(Size availableSize)
        {
            _typeface = null; // font properties may have changed — force rebuild
            RebuildFormattedText();

            double w = double.IsNaN(FixedWidth)  ? (_formatted?.Width  ?? 0) : FixedWidth;
            double h = double.IsNaN(FixedHeight) ? (_formatted?.Height ?? 0) : FixedHeight;
            return new Size(w, h);
        }

        // ── Render ────────────────────────────────────────────────────────────

        protected override void OnRender(DrawingContext dc)
        {
            if (_formatted is null) return;

            double x = TextAlignment switch
            {
                TextAlignment.Center => (ActualWidth - _formatted.Width) / 2.0,
                TextAlignment.Right  => ActualWidth  - _formatted.Width,
                _                    => 0.0,
            };
            double y = (ActualHeight - _formatted.Height) / 2.0;

            dc.DrawText(_formatted, new Point(Math.Max(0, x), Math.Max(0, y)));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void RebuildFormattedText()
        {
            _typeface ??= new Typeface(
                FontFamily,
                FontStyles.Normal,
                FontWeight,
                FontStretches.Normal);

            if (_pixelsPerDip == 0)
                _pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            _formatted = new FormattedText(
                Text ?? string.Empty,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                _typeface,
                FontSize,
                Foreground,
                _pixelsPerDip);
        }
    }
}
