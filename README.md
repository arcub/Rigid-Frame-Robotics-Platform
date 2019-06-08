# Rigid-Frame-Robotics-Platform
Open Source project by Andrew Forster
08 June 2019
This is the simulation side of a robot control system.
A framework (and 3d models) that represent the robot to be controlled is simulated.
Sections of the framework are designated as points where angles are measured, which also correspond to servo locations on the physical (or further simulated!) robot.
Some rudimentry socket networking code then sends a data packet of those angles over wifi to a connected robot.

To be added: The python script that runs on a Raspberry Pi Zero W, that accepts the data packets and in turn sets the connects servos to their positions.

Still in progress: The balance system is in the process of being upgraded. Right now it's a mix-together of very adhoc systems (pretty much my style of coding anywat) that need cleaning up.

Future plans: I would like to take advantage of Windows 10 IoT for the Raspberry Pi and Unity's networking protocols. If I can the IoT to act as a server, in that it waits for players/clients to connect. In this case it would be a Unity version of the simulation on a more powerful computer.
This provides the benefit that when the robot is switched on and finishes booting up, it'll appear on the network to any clients wishing to log onto it, much like a Minecraft server would appear.
It also provides the chance for simulated robots in simulated environments, especially since Unity can take a camera rendered scene and process it like a texture. Which would allow it to send that texture like a video stream would be from a physical robot with cameras.
Leading to the ability to test the robot in far wider areas.
