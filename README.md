<h1 style="text-align: center;">MicroCodeGenerator</h1>
<h2 style="text-align: center;">Tool to edit the MicroCodes for the TTL 6510 Computer</h2>
<br>
<div style="text-align: center;">
  <img src="docs/assets/images/main_screen.png" />
</div>
<br>
First, we have to look at the schematic of the micro code sequencer of the TTL 6510 computer.
<br><br>
<div style="text-align: center;">
  <img src="docs/assets/images/schematic.png" />
</div>
<br>
There are 2 ROMs in parallel to produce 16 bit wide micro codes. An instruction register provides 8 bit of the address. But the micro steps are created by a 4-bit counter and a clock phase. This together allow for 5-bit for the micro steps, giving 16 clocks or 32 phases for each instruction. The 6502 would normally only use up to 7 clocks per instruction. But I decided to implement BCD calculations with more microsteps instead of more hardware, I gave one more bit to the microsteps.
<br><br>
<div style="text-align: center;">
  <img src="docs/assets/images/ROM_signals.png" />
</div>
<br>
If you look closer, the instruction and the microsteps generate 13 bits of the address bus. That leaves 4 bits for additional conditions. The highest bit is reserved for exceptions, like interrupts and reset, because they will get the highest priority. Then there are 3 bits split into any special condition and then full carry and half carry which also can have double meaning. Those bits are generated for address calculations or branch conditions.
<br><br>
<div style="text-align: center;">
  <img src="docs/assets/images/jmp_example.png" />
</div>
<br>
The GUI shows an Excel-like grid with a description of the instruction on the left and cells for the micro code phases to the right. <br>
Each instruction starts with T0 for fetching the instruction code and ends with fecthing the next instruction code at the last clock phase. The microstep counter is loaded to T1 with /LD_IR, continuing at T2 of the next instruction cycle.<br>
Each instruction uses 5 rows for contents plus one row for spacing. The top row is a text field for a comment. The next 4 rows represent the codes for the 4 signal groups:<br>
- Address output<br>
- Internal databus enable<br>
- Load internal register<br>
- ALU-code<br>
The JMP instruction shown here is very simple. 3 bytes have to be read: the instrcution code, the low part of the address and the higher 8-bit of the address. Each memory read takes one clock for the 6502. The first phase has to send out the address and the second phase of the clock phase is used to read from memory. To open the databus driver from external memory to internal databus, /OE_iDB ha to be activated. The load signals /LD_IR, /LD_AL and /LD_PC_H take care of capturing the internal databus contents at the rising edge at the end of the phase into the correct register.
<br>
<br>
<div style="text-align: center;">
  <img src="docs/assets/images/combobox.png" />
</div>
<br>
When clicking on one of the selection cells, a combobox is opened to chose from the possible codes. This helps chosing only correct codes.
<br>
<br>
<div style="text-align: center;">
  <img src="docs/assets/images/area_selection.png" />
</div>
<br>
To select individual areas for the higher bits, there is a combobox at the bottom. You can check the microcodes for the different bit combinations.
<br>
<br>
<div style="text-align: center;">
  <img src="docs/assets/images/exceptions.png" />
</div>
<br>
This area shows the exception area for the bit combinations where the 3 low active signals RESET, NMI and INT replace the instruction register. The ALU code in this specific case doesn't select an ALU function but the low address part of the vector address.
<br>
<br>
<div style="text-align: center;">
  <img src="docs/assets/images/one_instruction.png" />
</div>
<br>
The last area shows one instruction for all 3 higher bits (8 special conditions). This example shows the BVC instruction. The top rows here show the microcodes, when the branch condition is not met and the execution will continue just after the branch. The hardware sets the "SPEC_COND" bit when the overflow flag is cleared with the /OE_PC_L_BR signal in phase T1_1 and the lower rows will be executed with 1 phase latency to execute the branch from T2_1 on.
<br>
<br>
<div style="text-align: center;">
  <img src="docs/assets/images/ADC_BCD.png" />
</div>
<br>
This last example shows all lower rows the ADC # instruction to be executed when the BCD flag is set resulting in setting "SPEC_COND". In this case additional microcodes will have to be executed to check and perform half and full byte correction by adding 0x06 and/or 0x60 to keep the result in the BCD range.<br>
ADC and SBC in BCD mode are the only instructions to not meet the original 6502 cycles to save some chips in hardware. But as far as I could find, these conditions are seldomly used.