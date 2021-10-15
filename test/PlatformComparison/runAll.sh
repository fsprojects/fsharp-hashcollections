cd FSharpTest
echo "Dotnet version:" `dotnet --version`
dotnet run -c Release
cd ..
cd ScalaTest
./run.sh
cd ..