#requires -Version 5
<#
  WT Launcher 配置与规则验证门。
  校验：config 可解析、字段完整、id 唯一、危险命令识别、命令行转义规则。
  directory 存在性默认仅警告（路径绑定本机，换机不可移植），-CheckDirs 可转为错误。
#>
param([switch]$CheckDirs)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$cfg = Join-Path $root 'config\config.json'
$exitCode = 0

function Fail([string]$msg) { Write-Host "FAIL: $msg" -ForegroundColor Red;   $script:exitCode = 1 }
function Ok  ([string]$msg) { Write-Host "OK  : $msg" -ForegroundColor Green }

# 1. 配置可解析
if (-not (Test-Path $cfg)) { Fail "config.json 不存在: $cfg"; exit 1 }
try {
  $items = Get-Content $cfg -Raw -Encoding UTF8 | ConvertFrom-Json
} catch {
  Fail "config.json 解析失败: $_"; exit 1
}
Ok "config.json 解析成功，共 $($items.Count) 项"

# 2. 字段完整性
$bad = $items | Where-Object {
  -not $_.name -or -not $_.directory -or -not $_.command -or
  ($_.PSObject.Properties.Name -notcontains 'confirm') -or -not $_.id
}
if ($bad) { Fail "$($bad.Count) 项缺少必要字段(name/directory/command/confirm/id)" }
else { Ok "所有项字段完整" }

# 3. id 唯一
$dup = $items.id | Group-Object | Where-Object { $_.Count -gt 1 }
if ($dup) { Fail "存在重复 id: $($dup.Name -join ', ')" }
else { Ok "id 唯一" }

# 4. 危险命令识别（与 HTA isDangerous 同规则）
$dangerPat = 'dangerously|yolo|skip-permissions|bypass-approvals|bypass-sandbox'
$danger = $items | Where-Object { $_.command -match $dangerPat }
$settingsFile = Join-Path $root 'config\settings.json'
$ce = $false
if (Test-Path $settingsFile) { try { $ce = [bool](Get-Content $settingsFile -Raw -Encoding UTF8 | ConvertFrom-Json).confirmEnabled } catch {} }
$dangerNoConfirm = $danger | Where-Object { $_.confirm -eq $false }
if ($ce) {
  Write-Host "INFO: $($danger.Count) 项含危险标志；confirmEnabled=true，运行时强制确认（含 $($dangerNoConfirm.Count) 项 confirm=false）" -ForegroundColor Yellow
} else {
  Write-Host "INFO: $($danger.Count) 项含危险标志；confirmEnabled=false，运行时不弹窗（$($dangerNoConfirm.Count) 项危险且 confirm=false 将直接启动）" -ForegroundColor Yellow
}
if ($dangerNoConfirm) {
  $dangerNoConfirm | ForEach-Object { Write-Host "      - $($_.name)" -ForegroundColor Yellow }
}

# 5. 转义规则断言（复刻 HTA quoteArg：反斜杠遇引号或末尾翻倍）
function QuoteArg([string]$s) {
  $out = '"'; $bs = 0
  foreach ($ch in $s.ToCharArray()) {
    if ($ch -eq '\') { $bs++; continue }
    if ($ch -eq '"') { $out += ('\' * (2 * $bs)) + '\"'; $bs = 0; continue }
    $out += ('\' * $bs) + $ch; $bs = 0
  }
  $out += ('\' * (2 * $bs)) + '"'
  return $out
}
$bs = [char]92  # 反斜杠，用字符构造避免字符串歧义
# 规则：中间反斜杠不翻倍；遇引号或位于尾部时翻倍
$cases = @(
  @{ in = 'ab';                            want = '"ab"' },
  @{ in = 'a' + $bs + 'b';                 want = '"a' + $bs + 'b"' },          # 中间反斜杠，不翻倍
  @{ in = 'a"b';                           want = '"a' + $bs + '"b"' },         # 引号转义
  @{ in = 'a' + $bs + 'b' + $bs;           want = '"a' + $bs + 'b' + $bs + $bs + '"' }  # 尾部反斜杠，翻倍
)
foreach ($c in $cases) {
  $got = QuoteArg $c.in
  if ($got -ne $c.want) { Fail "quoteArg('$($c.in)') = '$got'，期望 '$($c.want)'" }
}
if ($exitCode -eq 0) { Ok "quoteArg 转义规则断言通过" }

# 6. 目录存在性（可选）
$missing = $items | Where-Object { -not (Test-Path $_.directory) }
if ($CheckDirs) {
  if ($missing) { Fail "$($missing.Count) 项目录不存在" } else { Ok "所有目录存在" }
} elseif ($missing) {
  Write-Host "WARN(非阻塞): $($missing.Count) 项目录在当前机器不存在（-CheckDirs 可转为错误）" -ForegroundColor Yellow
}

if ($exitCode -eq 0) { Write-Host "`n验证通过" -ForegroundColor Green }
else { Write-Host "`n验证失败" -ForegroundColor Red }
exit $exitCode
