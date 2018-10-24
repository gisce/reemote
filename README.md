## REEMote

Access to remote meters using IEC870REE protocol

## How to hack in it?

1. Install mono (`apt-get install mono-complete`)
2. Clone this repo
3. Use your favorite editor to code, you can try [Visual Studio Code](https://code.visualstudio.com/)

## How to use it?

1. Build the ReemoteWrapper object
    - ReemoteWrapper(IP, PORT, LINK ADDR, MPOINT, PASS, DATE FROM, DATE TO, BILLINGS/PROFILES, REQUEST, CONTRACTS)
        - BILLINGS/PROFILES = "b" / "p"
        - REQUEST = "0", "1", "2", "3", "4"
        - CONTRACTS = List with 1, 2 or/and 3
    - Example:
        - ReemoteWrapper("xx.xx.xx.xxx", 20000, 1, 1, 1, "2017-11-01T00:00:00", "2017-11-30T00:00:00", "b", "1", [1,2,3])

2. Execute command
    - Use execute_request() method
