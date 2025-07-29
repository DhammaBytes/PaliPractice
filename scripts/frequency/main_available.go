package main

import "dpd/go_modules/tools"

// Modified version that processes all available corpuses
// CST (needs conversion from XML first), BJT, and SYA are available
func main() {
	tools.PTitle("saving frequency files and word lists (available corpuses)")

	tic := tools.Tic()

	// Process available corpuses
	makeCstFreq()  // Will work if XML files were converted to txt
	makeBjtFreq()  // Should work with BJT Roman text files
	makeSyaFreq()  // Should work with syāmaraṭṭha_1927 text files
	
	tic.Toc()
}
