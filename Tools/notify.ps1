param(
    [string]$Title = 'Claude Code',
    [string]$Message = 'Готово, жду тебя'
)
# Сигнал пользователю: звук + всплывающее уведомление Windows.
# Вызывается хуками Stop и Notification из .claude/settings.json.
# Ошибки глушим: уведомление не должно ломать работу Claude.
try {
    [System.Media.SystemSounds]::Asterisk.Play()
    Add-Type -AssemblyName System.Windows.Forms, System.Drawing
    $icon = New-Object System.Windows.Forms.NotifyIcon
    $icon.Icon = [System.Drawing.SystemIcons]::Information
    $icon.BalloonTipTitle = $Title
    $icon.BalloonTipText = $Message
    $icon.Visible = $true
    $icon.ShowBalloonTip(5000)
    Start-Sleep -Seconds 4
    $icon.Dispose()
} catch { }
exit 0
