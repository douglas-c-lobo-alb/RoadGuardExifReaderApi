#!/bin/bash

apt-get update
apt-get install -y git cmake make gcc libtool

if [ ! -f /usr/local/lib/libh3.so ]; then
  git clone https://github.com/uber/h3.git /tmp/h3
  cd /tmp/h3
  git checkout v4.4.1
  mkdir build
  cd build
  cmake -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON ..
  make -j$(nproc)
  make install
  echo "/usr/local/lib" > /etc/ld.so.conf.d/h3.conf
  ldconfig
fi
cd /src

