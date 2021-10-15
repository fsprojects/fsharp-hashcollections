cd FSharpTest
echo "Dotnet version:" `dotnet --version`
dotnet run -c Release
cd ..
cd ScalaTest
scala --version
scalac ./Program.scala && scala Program
cd ..