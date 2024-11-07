# LoggerLib
 a small library to make it easier to build a console application

 it makes it possible to read from the console whilst writing to it

 <details>
 <summary>how it works</summary>
  <ol>
   <li>when your code calls readline, it starts a loop, for each itteration it asks the console to wait for the user to input a key.</li>
   <li>when the user presses a key, the code checks if it's the key <code>return</code> if it is, it should return the current input.</li>
  </ol>
  
  <br/>
  <ul>
    <li> if your code calls write or writeline whilst a readline is being run, it does the following: </li>
  </ul>
  <ol>
   <li>clear the current line</li>
   <li>move the cursor to the start of the current line</li>
   <li>print the value to be printed</li>
   <li>move the cursor down a line</li>
   <li>print the current input (the text you had already wrote to the console before the write call)</li>
  </ol>
 </details>
