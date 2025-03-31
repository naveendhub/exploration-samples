package main

import (
	"bufio"
	"fmt"
	"os"
)

type Vertex struct {
	X int
	Y int
}

func main() {

	// open the file
	file, err := os.Open("test.txt")

	// Check for the error that occurred during the opening of the file
	if err != nil {
		fmt.Println(err)
	}

	// Close the file at the end of the program
	defer file.Close()

	// create a new scanner
	scanner := bufio.NewScanner(file)

	// Use scanword to split
	scanner.Split(bufio.ScanWords)
	for scanner.Scan() {
		fmt.Println(scanner.Text())
	}

	// check for the error that occurred during the scanning
	if err := scanner.Err(); err != nil {
		fmt.Println(err)
	}
}