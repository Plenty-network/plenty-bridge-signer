FROM mcr.microsoft.com/dotnet/runtime:5.0
RUN mkdir /Data
VOLUME /Data
ENV LiteDB__Path="/Data/state.db"
COPY bin/Release/net5.0/publish/ /App/
WORKDIR /App
ENTRYPOINT ["dotnet", "Signer.Service.dll"]
