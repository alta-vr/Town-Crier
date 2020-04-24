FROM ubuntu

RUN apt update 
RUN apt-get install -y libssl1.1

WORKDIR /data
COPY build /data
RUN chmod -R 777 .

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT="true"
ENV USE_ENV_ALTA_LOGIN="true"

CMD ["/data/Town-Crier"]