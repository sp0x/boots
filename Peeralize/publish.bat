rd published /s /q
dotnet restore
dotnet publish -c Debug -o published

tar -zcvf published.tar.gz published
scp published.tar.gz vasko@212.70.148.135:published.tar.gz
