export function initializeFilePaste(fileDropContainer, inputFile) {

    function onDrop(event) {
        event.preventDefault(); 
        fileDropContainer.classList.remove('dragover'); 

        const droppedFiles = event.dataTransfer.files;

   
        inputFile.files = droppedFiles;

        const changeEvent = new Event('change', { bubbles: true });
        inputFile.dispatchEvent(changeEvent);
    }




    fileDropContainer.addEventListener('drop', onDrop);

    fileDropContainer.addEventListener('click', onClick);
    function onClick(event) {
        inputFile.click();
    }
    return {
        dispose: () => {
            fileDropContainer.removeEventListener('click', onClick);
            fileDropContainer.removeEventListener('dragover', onDragOver);
        }
    };

}