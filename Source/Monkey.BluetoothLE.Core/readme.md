This project will contain the core contracts for the various classes and will be consumed from all projects.

This will enable us to build a cross-platform Bluetooth adn robotics library that conforms to these contracts.

By putting it in a separate project, we ensure that there will be no circular references.

TODO: rename to Core and get rid of the other core project

Device

 |-> one or more services [broad features]

   |-> one or more characteristics [different values within that feature]

     |-> values that can be read/notified/written

     |-> one or more descriptors that describe a characteristic

