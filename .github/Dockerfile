FROM ubuntu:22.04

RUN apt-get update && apt-get install -y \
    wget curl unzip git libgtk-3-dev libgl1-mesa-dev libglu1-mesa xvfb tar

COPY Unity_6000.1.6f1.tar.gz /tmp/unity.tar.gz

RUN mkdir -p /opt/unity \
    && tar -xzf /tmp/unity.tar.gz -C /opt/unity \
    && rm /tmp/unity.tar.gz

ENV PATH="/opt/unity/Editor:/opt/unity/Editor/Data/PlaybackEngines/LinuxStandaloneSupport:$PATH"
