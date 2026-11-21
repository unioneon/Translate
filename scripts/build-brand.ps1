$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path $PSScriptRoot
$assets=Join-Path $root 'src\assets'
New-Item -ItemType Directory -Force -Path $assets | Out-Null
$bitmap=[Drawing.Bitmap]::new(256,256)
$graphics=[Drawing.Graphics]::FromImage($bitmap);$graphics.SmoothingMode='AntiAlias';$graphics.Clear([Drawing.Color]::Transparent)
$shape=[Drawing.Drawing2D.GraphicsPath]::new()
foreach($arc in @(@(6,6,100,100,180),@(150,6,100,100,270),@(150,150,100,100,0),@(6,150,100,100,90))){$shape.AddArc($arc[0],$arc[1],$arc[2],$arc[3],$arc[4],90)};$shape.CloseFigure()
$gradient=[Drawing.Drawing2D.LinearGradientBrush]::new([Drawing.Point]::new(0,0),[Drawing.Point]::new(256,256),[Drawing.Color]::FromArgb(0,162,255),[Drawing.Color]::FromArgb(0,102,245))
$graphics.FillPath($gradient,$shape)
$pen=[Drawing.Pen]::new([Drawing.Color]::White,11);$pen.StartCap='Round';$pen.EndCap='Round';$pen.LineJoin='Round'
$graphics.DrawEllipse($pen,52,52,152,152);$graphics.DrawEllipse($pen,94,52,68,152);$graphics.DrawLine($pen,54,128,202,128)
$bitmap.Save((Join-Path $assets 'app.png'),[Drawing.Imaging.ImageFormat]::Png)
$png=[IO.MemoryStream]::new();$bitmap.Save($png,[Drawing.Imaging.ImageFormat]::Png);$bytes=$png.ToArray()
$file=[IO.File]::Create((Join-Path $assets 'app.ico'));$writer=[IO.BinaryWriter]::new($file)
$writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]1);$writer.Write([byte]0);$writer.Write([byte]0);$writer.Write([byte]0);$writer.Write([byte]0);$writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$bytes.Length);$writer.Write([uint32]22);$writer.Write($bytes);$writer.Dispose();$png.Dispose()
$pen.Dispose();$gradient.Dispose();$shape.Dispose();$graphics.Dispose();$bitmap.Dispose()
Write-Output 'Generated Translate app icon.'
$images=Join-Path $root 'docs\design';New-Item -ItemType Directory -Path $images -Force|Out-Null
$wizard=[Drawing.Bitmap]::new(410,785);$g=[Drawing.Graphics]::FromImage($wizard);$g.SmoothingMode='AntiAlias';$g.TextRenderingHint='AntiAliasGridFit'
$background=[Drawing.Drawing2D.LinearGradientBrush]::new([Drawing.Point]::new(0,0),[Drawing.Point]::new(410,785),[Drawing.Color]::FromArgb(0,155,255),[Drawing.Color]::FromArgb(18,74,213));$g.FillRectangle($background,0,0,410,785)
$logo=[Drawing.Image]::FromFile((Join-Path $assets 'app.png'));$g.DrawImage($logo,43,80,160,160)
$white=[Drawing.SolidBrush]::new([Drawing.Color]::White);$soft=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(205,233,255));$font=[Drawing.Font]::new('Segoe UI',39,[Drawing.FontStyle]::Bold);$body=[Drawing.Font]::new('Segoe UI',19)
$g.DrawString('Translate',$font,$white,42,270);$g.DrawString("Small window.`nBig world.",$body,$soft,45,353);$g.DrawString("ONE FIELD.`nELEVEN LANGUAGES.",[Drawing.Font]::new('Segoe UI',12,[Drawing.FontStyle]::Bold),$soft,47,680)
$wizard.Save((Join-Path $images 'wizard-large.bmp'),[Drawing.Imaging.ImageFormat]::Bmp);$g.Dispose();$wizard.Dispose();$background.Dispose();$font.Dispose();$body.Dispose();$white.Dispose();$soft.Dispose()
$small=[Drawing.Bitmap]::new(64,64);$sg=[Drawing.Graphics]::FromImage($small);$sg.Clear([Drawing.Color]::White);$sg.DrawImage($logo,0,0,64,64);$small.Save((Join-Path $images 'wizard-small.bmp'),[Drawing.Imaging.ImageFormat]::Bmp);$sg.Dispose();$small.Dispose();$logo.Dispose()
