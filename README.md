# MCUSimulator

A highly extensible assembly level debugger and simulator for microcontroller.

# Motivation
I needed to build a solution that needs to be very economical in terms of cost. While shopping for a microcontroller, I came across the 3cent padauk PMS150 which nicely fits the bill. But there was a catch, I nneded to but their programmer and ICE that is just too costly for my need.
Luckily I came across the community developed programmer "EASYPDKPROGRAMMER" and SDCC support was also added.
After building my hardware and its software, there was a bug that needs to be resolved and blowing through my hundreds of OTP chip wasnt sitting well with me.

So I detourded and spent some 3 days building this assembly level debugger and simulator for PDK13. The build was loosely decoupled enough for anyone to add support for other MCU. 8051 support is currently in progress.

You are welcome to contribute.
