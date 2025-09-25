using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Linq;

namespace SPRDNVEditor
{
    public partial class HexEditor : UserControl
    {
        private byte[] _data;
        private int _bytesPerLine = 16;
        private int _selectedByte = -1;
        private bool _isEditing = false;
        private string _editBuffer = "";
        
        private Font _hexFont;
        private SolidBrush _textBrush;
        private SolidBrush _selectedBrush;
        private SolidBrush _editingBrush;
        private SolidBrush _backgroundBrush;
        private SolidBrush _offsetBrush;
        private SolidBrush _whiteBrush;
        private Pen _gridPen;
        
        private int _charWidth;
        private int _charHeight;
        private int _offsetWidth;
        private int _hexAreaStart;
        private int _hexAreaWidth;
        private int _textAreaStart;
        
        private VScrollBar _vScrollBar;
        private int _topLine = 0;
        private int _visibleLines;
        
        private System.Windows.Forms.Timer _cursorTimer;
        private bool _cursorVisible = true;

        public event EventHandler<byte[]> DataChanged;

        public byte[] Data
        {
            get => _data;
            set
            {
                _data = value ?? new byte[0];
                _selectedByte = -1;
                _isEditing = false;
                _editBuffer = "";
                UpdateScrollBar();
                Invalidate();
            }
        }

        public int SelectedByteIndex
        {
            get => _selectedByte;
            set
            {
                if (value >= 0 && value < (_data?.Length ?? 0))
                {
                    _selectedByte = value;
                    _isEditing = false;
                    _editBuffer = "";
                    EnsureByteVisible(value);
                    Invalidate();
                }
            }
        }

        public HexEditor()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | 
                    ControlStyles.DoubleBuffer | ControlStyles.Selectable | 
                    ControlStyles.ResizeRedraw, true);
            
            _hexFont = new Font("Consolas", 10, FontStyle.Regular);
            _textBrush = new SolidBrush(Color.Black);
            _selectedBrush = new SolidBrush(Color.FromArgb(51, 153, 255));
            _editingBrush = new SolidBrush(Color.FromArgb(255, 220, 220));
            _backgroundBrush = new SolidBrush(Color.White);
            _offsetBrush = new SolidBrush(Color.Gray);
            _whiteBrush = new SolidBrush(Color.White);
            _gridPen = new Pen(Color.LightGray);
            
            this.TabStop = true;
            
            CalculateMetrics();
            CreateScrollBar();
            CreateCursorTimer();
            
            this.Resize += (s, e) => UpdateLayout();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.White;
            this.Size = new Size(600, 400);
            this.TabStop = true;
            this.Cursor = Cursors.IBeam;
            this.ResumeLayout();
        }

        private void CreateCursorTimer()
        {
            _cursorTimer = new System.Windows.Forms.Timer();
            _cursorTimer.Interval = 500;
            _cursorTimer.Tick += (s, e) =>
            {
                if (_isEditing)
                {
                    _cursorVisible = !_cursorVisible;
                    InvalidateSelectedByte();
                }
            };
        }

        private void InvalidateSelectedByte()
        {
            if (_selectedByte < 0 || _data == null) return;
            
            int line = _selectedByte / _bytesPerLine - _topLine;
            if (line >= 0 && line < _visibleLines)
            {
                int y = 5 + line * _charHeight;
                int x = _hexAreaStart + (_selectedByte % _bytesPerLine) * (_charWidth * 3);
                var rect = new Rectangle(x - 2, y - 2, _charWidth * 3, _charHeight + 4);
                Invalidate(rect);
            }
        }

        private void CalculateMetrics()
        {
            using (var g = CreateGraphics())
            {
                var size = g.MeasureString("W", _hexFont);
                _charWidth = (int)Math.Ceiling(size.Width);
                _charHeight = (int)Math.Ceiling(size.Height);
            }
            
            _offsetWidth = _charWidth * 10; // "00000000: "
            _hexAreaStart = _offsetWidth;
            _hexAreaWidth = _charWidth * (_bytesPerLine * 3 - 1); // "00 00 00..."
            _textAreaStart = _offsetWidth + _hexAreaWidth + _charWidth * 2;
        }

        private void CreateScrollBar()
        {
            _vScrollBar = new VScrollBar
            {
                Dock = DockStyle.Right,
                SmallChange = 1,
                LargeChange = 10
            };
            _vScrollBar.Scroll += VScrollBar_Scroll;
            this.Controls.Add(_vScrollBar);
        }

        private void UpdateLayout()
        {
            _visibleLines = (Height - 10) / _charHeight;
            UpdateScrollBar();
            Invalidate();
        }

        private void UpdateScrollBar()
        {
            if (_data == null || _data.Length == 0)
            {
                _vScrollBar.Enabled = false;
                return;
            }

            int totalLines = (_data.Length + _bytesPerLine - 1) / _bytesPerLine;
            _vScrollBar.Enabled = totalLines > _visibleLines;
            
            if (_vScrollBar.Enabled)
            {
                _vScrollBar.Maximum = Math.Max(0, totalLines - 1);
                _vScrollBar.LargeChange = Math.Max(1, _visibleLines);
                _vScrollBar.Value = Math.Min(_topLine, Math.Max(0, _vScrollBar.Maximum - _vScrollBar.LargeChange + 1));
            }
        }

        private void VScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            _topLine = e.NewValue;
            Invalidate();
        }

        private void EnsureByteVisible(int byteIndex)
        {
            int line = byteIndex / _bytesPerLine;
            if (line < _topLine)
            {
                _topLine = line;
                if (_vScrollBar.Enabled) _vScrollBar.Value = _topLine;
                Invalidate();
            }
            else if (line >= _topLine + _visibleLines)
            {
                _topLine = line - _visibleLines + 1;
                if (_vScrollBar.Enabled) _vScrollBar.Value = _topLine;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            var g = e.Graphics;
            g.FillRectangle(_backgroundBrush, ClientRectangle);
            
            if (_data == null || _data.Length == 0)
            {
                g.DrawString("No data loaded", _hexFont, _textBrush, 10, 10);
                return;
            }

            int y = 5;
            int startLine = _topLine;
            int endLine = Math.Min(startLine + _visibleLines, (_data.Length + _bytesPerLine - 1) / _bytesPerLine);

            for (int line = startLine; line < endLine; line++)
            {
                DrawLine(g, line, y);
                y += _charHeight;
            }
        }

        private void DrawLine(Graphics g, int line, int y)
        {
            int startByte = line * _bytesPerLine;
            int endByte = Math.Min(startByte + _bytesPerLine, _data.Length);
            
            // Draw offset
            string offset = $"{startByte:X8}: ";
            g.DrawString(offset, _hexFont, _offsetBrush, 5, y);
            
            // Draw hex bytes
            int hexX = _hexAreaStart;
            for (int i = startByte; i < endByte; i++)
            {
                bool isSelected = (i == _selectedByte);
                bool isEditing = isSelected && _isEditing;
                
                // Draw background
                if (isEditing)
                {
                    var bgRect = new Rectangle(hexX - 1, y - 1, _charWidth * 2 + 2, _charHeight + 2);
                    g.FillRectangle(_editingBrush, bgRect);
                }
                else if (isSelected)
                {
                    var bgRect = new Rectangle(hexX - 1, y - 1, _charWidth * 2 + 2, _charHeight + 2);
                    g.FillRectangle(_selectedBrush, bgRect);
                }
                
                // Draw hex value
                string hexByte;
                if (isEditing && _editBuffer.Length > 0)
                {
                    hexByte = _editBuffer.PadRight(2, '_');
                }
                else
                {
                    hexByte = $"{_data[i]:X2}";
                }
                
                var brush = (isSelected && !isEditing) ? _whiteBrush : _textBrush;
                g.DrawString(hexByte, _hexFont, brush, hexX, y);
                
                // Draw cursor
                if (isEditing && _cursorVisible && _editBuffer.Length < 2)
                {
                    int cursorX = hexX + (_editBuffer.Length * _charWidth);
                    g.DrawLine(Pens.Black, cursorX, y, cursorX, y + _charHeight);
                }
                
                hexX += _charWidth * 3;
            }
            
            // Draw ASCII text
            int textX = _textAreaStart;
            for (int i = startByte; i < endByte; i++)
            {
                char c = (char)_data[i];
                string ch = (char.IsControl(c) || c > 127) ? "." : c.ToString();
                
                if (i == _selectedByte)
                {
                    var bgRect = new Rectangle(textX - 1, y - 1, _charWidth + 2, _charHeight + 2);
                    g.FillRectangle(_isEditing ? _editingBrush : _selectedBrush, bgRect);
                    g.DrawString(ch, _hexFont, _isEditing ? _textBrush : _whiteBrush, textX, y);
                }
                else
                {
                    g.DrawString(ch, _hexFont, _textBrush, textX, y);
                }
                
                textX += _charWidth;
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            
            if (_data == null || _data.Length == 0)
                return;

            int line = (e.Y - 5) / _charHeight + _topLine;
            int startByte = line * _bytesPerLine;
            
            // Check if click is in hex area
            if (e.X >= _hexAreaStart && e.X < _hexAreaStart + _hexAreaWidth)
            {
                int relativeX = e.X - _hexAreaStart;
                int bytePos = relativeX / (_charWidth * 3);
                int byteIndex = startByte + bytePos;
                
                if (byteIndex >= 0 && byteIndex < _data.Length)
                {
                    _selectedByte = byteIndex;
                    _isEditing = false;
                    _editBuffer = "";
                    _cursorTimer.Stop();
                    this.Focus();
                    Invalidate();
                }
            }
            // Check if click is in text area
            else if (e.X >= _textAreaStart)
            {
                int textPos = (e.X - _textAreaStart) / _charWidth;
                int byteIndex = startByte + textPos;
                
                if (byteIndex >= 0 && byteIndex < _data.Length)
                {
                    _selectedByte = byteIndex;
                    _isEditing = false;
                    _editBuffer = "";
                    _cursorTimer.Stop();
                    this.Focus();
                    Invalidate();
                }
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            
            if (_selectedByte >= 0 && _selectedByte < (_data?.Length ?? 0))
            {
                StartEditing();
            }
        }

        private void StartEditing()
        {
            _isEditing = true;
            _editBuffer = "";
            _cursorVisible = true;
            _cursorTimer.Start();
            Invalidate();
        }

        private void StopEditing(bool save)
        {
            if (!_isEditing) return;
            
            _cursorTimer.Stop();
            
            if (save && _editBuffer.Length > 0)
            {
                // Parse the hex value
                string hexValue = _editBuffer;
                if (hexValue.Length == 1) hexValue = "0" + hexValue;
                
                if (byte.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out byte newValue))
                {
                    _data[_selectedByte] = newValue;
                    DataChanged?.Invoke(this, _data);
                }
            }
            
            _isEditing = false;
            _editBuffer = "";
            Invalidate();
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            
            if (_data == null || _data.Length == 0 || _selectedByte < 0)
                return;

            char c = char.ToUpper(e.KeyChar);
            
            // Handle hex input
            if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F'))
            {
                if (!_isEditing)
                {
                    StartEditing();
                }
                
                if (_editBuffer.Length < 2)
                {
                    _editBuffer += c;
                    _cursorVisible = true;
                    
                    if (_editBuffer.Length == 2)
                    {
                        // Auto-commit when we have 2 digits
                        StopEditing(true);
                        
                        // Move to next byte
                        if (_selectedByte < _data.Length - 1)
                        {
                            _selectedByte++;
                            EnsureByteVisible(_selectedByte);
                        }
                    }
                    else
                    {
                        InvalidateSelectedByte();
                    }
                }
                
                e.Handled = true;
            }
            else if (e.KeyChar == '\r' || e.KeyChar == '\n') // Enter
            {
                if (_isEditing)
                {
                    StopEditing(true);
                }
                else
                {
                    StartEditing();
                }
                e.Handled = true;
            }
            else if (e.KeyChar == '\x1b') // Escape
            {
                StopEditing(false);
                e.Handled = true;
            }
            else if (e.KeyChar == '\b') // Backspace
            {
                if (_isEditing && _editBuffer.Length > 0)
                {
                    _editBuffer = _editBuffer.Substring(0, _editBuffer.Length - 1);
                    InvalidateSelectedByte();
                }
                e.Handled = true;
            }
            else if (e.KeyChar == ' ') // Space to start editing
            {
                if (!_isEditing)
                {
                    StartEditing();
                }
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            if (_data == null || _data.Length == 0)
                return;

            bool handled = true;
            
            switch (e.KeyCode)
            {
                case Keys.Left:
                    if (_isEditing)
                    {
                        StopEditing(true);
                    }
                    if (_selectedByte > 0)
                    {
                        _selectedByte--;
                        EnsureByteVisible(_selectedByte);
                        Invalidate();
                    }
                    break;
                    
                case Keys.Right:
                    if (_isEditing)
                    {
                        StopEditing(true);
                    }
                    if (_selectedByte < _data.Length - 1)
                    {
                        _selectedByte++;
                        EnsureByteVisible(_selectedByte);
                        Invalidate();
                    }
                    break;
                    
                case Keys.Up:
                    if (_isEditing)
                    {
                        StopEditing(true);
                    }
                    if (_selectedByte >= _bytesPerLine)
                    {
                        _selectedByte -= _bytesPerLine;
                        EnsureByteVisible(_selectedByte);
                        Invalidate();
                    }
                    break;
                    
                case Keys.Down:
                    if (_isEditing)
                    {
                        StopEditing(true);
                    }
                    if (_selectedByte + _bytesPerLine < _data.Length)
                    {
                        _selectedByte += _bytesPerLine;
                        EnsureByteVisible(_selectedByte);
                        Invalidate();
                    }
                    break;
                    
                case Keys.Home:
                    if (_isEditing)
                    {
                        StopEditing(true);
                    }
                    if (e.Control)
                    {
                        _selectedByte = 0;
                    }
                    else
                    {
                        _selectedByte = (_selectedByte / _bytesPerLine) * _bytesPerLine;
                    }
                    EnsureByteVisible(_selectedByte);
                    Invalidate();
                    break;
                    
                case Keys.End:
                    if (_isEditing)
                    {
                        StopEditing(true);
                    }
                    if (e.Control)
                    {
                        _selectedByte = _data.Length - 1;
                    }
                    else
                    {
                        int lineEnd = ((_selectedByte / _bytesPerLine) + 1) * _bytesPerLine - 1;
                        _selectedByte = Math.Min(lineEnd, _data.Length - 1);
                    }
                    EnsureByteVisible(_selectedByte);
                    Invalidate();
                    break;
                    
                case Keys.PageUp:
                    if (_isEditing)
                    {
                        StopEditing(true);
                    }
                    _selectedByte = Math.Max(0, _selectedByte - (_bytesPerLine * _visibleLines));
                    EnsureByteVisible(_selectedByte);
                    Invalidate();
                    break;
                    
                case Keys.PageDown:
                    if (_isEditing)
                    {
                        StopEditing(true);
                    }
                    _selectedByte = Math.Min(_data.Length - 1, _selectedByte + (_bytesPerLine * _visibleLines));
                    EnsureByteVisible(_selectedByte);
                    Invalidate();
                    break;
                    
                case Keys.Delete:
                    if (_selectedByte >= 0 && _selectedByte < _data.Length)
                    {
                        _data[_selectedByte] = 0x00;
                        DataChanged?.Invoke(this, _data);
                        Invalidate();
                    }
                    break;
                    
                case Keys.F2:
                    if (!_isEditing)
                    {
                        StartEditing();
                    }
                    break;
                    
                default:
                    handled = false;
                    break;
            }
            
            if (handled)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.Home:
                case Keys.End:
                case Keys.PageUp:
                case Keys.PageDown:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            StopEditing(true);
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cursorTimer?.Stop();
                _cursorTimer?.Dispose();
                _hexFont?.Dispose();
                _textBrush?.Dispose();
                _selectedBrush?.Dispose();
                _editingBrush?.Dispose();
                _backgroundBrush?.Dispose();
                _offsetBrush?.Dispose();
                _whiteBrush?.Dispose();
                _gridPen?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}