﻿export async function getEmailAdress() {
    try {
        // Make a GET request to the /.auth/me endpoint
        const response = await fetch("https://traspaso-cartera.azurewebsites.net/.auth/me", {
            method: "GET",
            headers: {
                "Content-Type": "application/json",
                // Add any additional headers as needed
            },
        });
        if (response.ok) {
            // Parse the JSON response
            const userData = await response.json()[0];
            const userClaims = userData.user_claims; // Assuming user_claims is the array containing claims
            const emailClaim = userClaims.find(claim => claim.typ === "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
            if (emailClaim) {
                return emailClaim.val;
            } else {
                console.error("Email address claim not found in response.");
                return null;
            }
        } else {
            console.error(`Failed to fetch email address: ${response.status} ${response.statusText}`);
            return null;
        }
    } catch (error) {
        console.error("Error fetching email address:", error);
        return null;
    }
}
