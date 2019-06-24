FROM mcr.microsoft.com/dotnet/core/sdk:2.2

COPY . /app

WORKDIR /app

RUN dotnet publish -c Release -r linux-x64

CMD /bin/bash