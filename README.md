# LoggerLib
 a library to make it easier to build a console application

 it makes it possible to read from the console whilst writing to it

## how it works:
 <details>
 <summary>Click to expand</summary>
  1. when your code calls readline, it starts a loop, for each itteration it asks the console to wait for the user to input a key. <br/>
  2. when the user presses a key, the code checks if it's the key <code>return</code> if it is, it should return the current input. <br/>
  
  <br/>
  <ul>
   <li> if your code calls write or writeline whilst a readline is being run, it does the following: </li>
  </ul>
  1. clear the current line <br/>
  2. move the cursor to the start of the current line <br/>
  3. print the value to be printed <br/>
  4. move the cursor down a line <br/>
  5. print the current input (the text you had already wrote to the console before the write call)
 </details>
