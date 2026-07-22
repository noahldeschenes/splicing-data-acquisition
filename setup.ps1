
$exePath = "C:\Path\To\YourApp.exe"

$arg1 = "FirstString"
$arg2 = "SecondString"
$arg3 = "ThirdString"


Start-Process -FilePath $exePath -ArgumentList $arg1, $arg2, $arg3 -NoNewWindow -Wait